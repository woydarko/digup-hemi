import {
  createPublicClient,
  createWalletClient,
  http,
  defineChain,
  verifyTypedData,
  isAddress,
  isHex,
  parseAbi,
  parseEventLogs,
  type Address,
  type Hex,
} from "viem";
import { privateKeyToAccount } from "viem/accounts";

export interface Env {
  ASSETS: Fetcher;
  NETWORK: string;
  CONTRACT: string;
  RELAYER_KEY: string;
}

const NETWORKS = {
  mainnet: { id: 43111, name: "Hemi", rpc: "https://rpc.hemi.network/rpc", explorer: "https://explorer.hemi.xyz" },
  testnet: {
    id: 743111,
    name: "Hemi Sepolia",
    rpc: "https://testnet.rpc.hemi.network/rpc",
    explorer: "https://testnet.explorer.hemi.xyz",
  },
  // anvil, for end-to-end tests on a local chain
  local: { id: 31337, name: "Anvil", rpc: "http://127.0.0.1:8545", explorer: "http://localhost" },
} as const;

const ABI = parseAbi([
  "function submitRun(address player, uint32 score, bool escaped, uint256 deadline, bytes signature)",
  "function getTop() view returns ((address player, uint32 score, uint64 timestamp)[])",
  "function nonces(address) view returns (uint256)",
  "function totalRuns() view returns (uint256)",
  "function totalEscapes() view returns (uint256)",
  "event BadgeMinted(address indexed player, uint256 indexed tokenId)",
]);

const RUN_TYPES = {
  Run: [
    { name: "player", type: "address" },
    { name: "score", type: "uint32" },
    { name: "escaped", type: "bool" },
    { name: "nonce", type: "uint256" },
    { name: "deadline", type: "uint256" },
  ],
} as const;

const MAX_SCORE = 1_000_000;
const DEADLINE_SECONDS = 15 * 60;
const RELAY_COOLDOWN_MS = 15_000;

// shortcut: per-isolate memory, so limits reset when Cloudflare spins a new isolate; upgrade to a
// Durable Object or the Rate Limiting binding if the relayer gets abused.
const lastRelay = new Map<string, number>();

function setup(env: Env) {
  const net = NETWORKS[env.NETWORK as keyof typeof NETWORKS] ?? NETWORKS.testnet;
  const chain = defineChain({
    id: net.id,
    name: net.name,
    nativeCurrency: { name: "Ether", symbol: "ETH", decimals: 18 },
    rpcUrls: { default: { http: [net.rpc] } },
    blockExplorers: { default: { name: "Hemi Explorer", url: net.explorer } },
  });
  const contract = env.CONTRACT as Address;
  const domain = { name: "DigUp", version: "1", chainId: net.id, verifyingContract: contract } as const;
  const publicClient = createPublicClient({ chain, transport: http() });
  return { net, chain, contract, domain, publicClient };
}

function json(data: unknown, status = 200): Response {
  return new Response(JSON.stringify(data, (_, v) => (typeof v === "bigint" ? v.toString() : v)), {
    status,
    headers: { "content-type": "application/json", "access-control-allow-origin": "*" },
  });
}

function parseScore(value: unknown): number | null {
  const n = Number(value);
  return Number.isInteger(n) && n >= 0 && n <= MAX_SCORE ? n : null;
}

async function handleApi(request: Request, env: Env, url: URL): Promise<Response> {
  const { net, chain, contract, domain, publicClient } = setup(env);
  const read = <T>(functionName: "getTop" | "nonces" | "totalRuns" | "totalEscapes", args: unknown[] = []) =>
    publicClient.readContract({ address: contract, abi: ABI, functionName, args } as never) as Promise<T>;

  if (request.method === "OPTIONS") {
    return new Response(null, {
      headers: {
        "access-control-allow-origin": "*",
        "access-control-allow-methods": "GET, POST",
        "access-control-allow-headers": "content-type",
      },
    });
  }

  if (url.pathname === "/api/config") {
    return json({ chainId: net.id, chainName: net.name, rpcUrl: net.rpc, explorer: net.explorer, contract });
  }

  if (url.pathname === "/api/top") {
    const [top, totalRuns, totalEscapes] = await Promise.all([
      read<readonly { player: Address; score: number; timestamp: bigint }[]>("getTop"),
      read<bigint>("totalRuns"),
      read<bigint>("totalEscapes"),
    ]);
    return json({ entries: top, totalRuns, totalEscapes });
  }

  // Ready-to-sign EIP-712 payload, so the browser only needs window.ethereum (no web3 library)
  if (url.pathname === "/api/typed-data") {
    const player = url.searchParams.get("player") ?? "";
    const score = parseScore(url.searchParams.get("score"));
    if (!isAddress(player) || score === null) return json({ error: "bad player or score" }, 400);
    const nonce = await read<bigint>("nonces", [player]);
    const deadline = BigInt(Math.floor(Date.now() / 1000) + DEADLINE_SECONDS);
    return json({
      domain,
      types: {
        EIP712Domain: [
          { name: "name", type: "string" },
          { name: "version", type: "string" },
          { name: "chainId", type: "uint256" },
          { name: "verifyingContract", type: "address" },
        ],
        ...RUN_TYPES,
      },
      primaryType: "Run",
      message: { player, score, escaped: url.searchParams.get("escaped") === "true", nonce, deadline },
    });
  }

  if (url.pathname === "/api/relay" && request.method === "POST") {
    const body = (await request.json().catch(() => null)) as Record<string, unknown> | null;
    const player = String(body?.player ?? "");
    const score = parseScore(body?.score);
    const escaped = body?.escaped === true;
    const signature = String(body?.signature ?? "");
    const deadline = BigInt(Number(body?.deadline ?? 0));
    const now = BigInt(Math.floor(Date.now() / 1000));
    if (!isAddress(player) || score === null || !isHex(signature) || deadline < now || deadline > now + 3600n) {
      return json({ ok: false, error: "invalid request" }, 400);
    }

    const ip = request.headers.get("cf-connecting-ip") ?? "local";
    const key = `${ip}|${player.toLowerCase()}`;
    const last = lastRelay.get(key) ?? 0;
    if (Date.now() - last < RELAY_COOLDOWN_MS) return json({ ok: false, error: "slow down, try again in a few seconds" }, 429);

    const nonce = await read<bigint>("nonces", [player]);
    const valid = await verifyTypedData({
      address: player,
      domain,
      types: RUN_TYPES,
      primaryType: "Run",
      message: { player, score, escaped, nonce, deadline },
      signature: signature as Hex,
    });
    if (!valid) return json({ ok: false, error: "signature does not match" }, 400);
    lastRelay.set(key, Date.now());

    const account = privateKeyToAccount(env.RELAYER_KEY as Hex);
    const wallet = createWalletClient({ account, chain, transport: http() });
    try {
      const { request: tx } = await publicClient.simulateContract({
        account,
        address: contract,
        abi: ABI,
        functionName: "submitRun",
        args: [player, score, escaped, deadline, signature as Hex],
      });
      const hash = await wallet.writeContract(tx);
      const receipt = await publicClient.waitForTransactionReceipt({ hash, timeout: 45_000 });
      const badge = parseEventLogs({ abi: ABI, logs: receipt.logs, eventName: "BadgeMinted" }).length > 0;
      return json({ ok: receipt.status === "success", txHash: hash, explorerUrl: `${net.explorer}/tx/${hash}`, badge });
    } catch (err) {
      return json({ ok: false, error: (err as Error).message.split("\n")[0] }, 502);
    }
  }

  return json({ error: "not found" }, 404);
}

export default {
  async fetch(request: Request, env: Env): Promise<Response> {
    const url = new URL(request.url);
    if (url.pathname.startsWith("/api/")) {
      try {
        return await handleApi(request, env, url);
      } catch (err) {
        return json({ ok: false, error: (err as Error).message.split("\n")[0] }, 500);
      }
    }
    return env.ASSETS.fetch(request);
  },
} satisfies ExportedHandler<Env>;
