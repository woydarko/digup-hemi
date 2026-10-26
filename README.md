# Dig Up

A 2D platformer where you dig your way up through a cave while lava rises beneath you. Break blocks, stomp
enemies, grab coins, and escape before the lava catches you.

Built with Unity 6 (C#), playable in the browser. Runs are saved onchain on **Hemi**.

**Play:** https://digup-hemi.digup-worker.workers.dev

**Contract (Hemi mainnet):** [`0x4999e6986165A727a358453797E824beec597575`](https://explorer.hemi.xyz/address/0x4999e6986165A727a358453797E824beec597575)
(the same address was used on Hemi testnet during development)

## Controls

| Action | Key |
|---|---|
| Move | A / D or ← / → |
| Jump | Space |
| Attack / break blocks | F |

## Hemi integration

- **Free to play, gasless saves.** When a run ends, the player signs an EIP-712 `Run` message in their wallet
  (no gas, no fee). A Cloudflare Worker checks the run and relays it to the `DigUp` contract, paying the gas.
  Only that relayer can submit runs; EOA and ERC-1271 smart-contract wallets can both sign.
- **Onchain leaderboard.** The contract keeps the top 10 best scores (one per wallet), shown on the main menu.
  The owner can `ban` a cheating wallet, which removes it from the board and blocks its later runs.
- **Soulbound "Escaped" badge.** Reaching the finish flag mints a non-transferable ERC-721 badge (once per wallet,
  ERC-5192 locked), with fully onchain SVG metadata.
- A wallet is only needed to save a run; the game itself is playable without one.

```
Unity WebGL ──jslib──▶ hemi.js (window.ethereum: sign) ──▶ Worker /api/* ──▶ DigUp.sol on Hemi
```

| Path | What |
|---|---|
| `onchain/contracts` | `DigUp.sol` (Foundry), tests, deploy script |
| `onchain/worker` | Cloudflare Worker: relayer API + hosting for the WebGL build |
| `Assets/Hemi/HemiBridge.cs` | In-game save prompt and leaderboard UI |
| `Assets/Plugins/WebGL/HemiBridge.jslib` | Unity → browser calls |
| `Assets/WebGLTemplates/Hemi` | WebGL page template with `hemi.js` |

## Build and deploy

1. **Contract**
   ```bash
   cd onchain/contracts && npm install && forge test
   # RELAYER = address of the Worker's relayer wallet; the deployer becomes the owner
   RELAYER=0x... forge script script/Deploy.s.sol --rpc-url hemi --account <keystore> --broadcast   # or hemi_testnet
   ```
2. **Unity**: open the project in Unity 6, *File → Build Profiles → Web*, template **Hemi**, build to `Build/WebGL`.
3. **Worker**: set `NETWORK` and `CONTRACT` in `onchain/worker/wrangler.toml`, then
   ```bash
   cd onchain/worker && npm install
   npx wrangler secret put RELAYER_KEY   # a dedicated wallet holding a little ETH on Hemi for gas
   npx wrangler deploy
   ```

| Network | Chain ID | RPC |
|---|---|---|
| Hemi mainnet | 43111 | https://rpc.hemi.network/rpc |
| Hemi testnet | 743111 | https://testnet.rpc.hemi.network/rpc |

## Known limits

- Scores are computed client-side, so the game cannot prove a run onchain. The relayer is the trust point: it
  rejects scores above `MAX_PLAUSIBLE_SCORE`, rate-limits per IP and per wallet, and the owner can `ban` any
  wallet that still gets a fake score through.
- Relayer rate limiting is in-memory per Worker isolate.

## License

Code is released under the [MIT License](LICENSE).
