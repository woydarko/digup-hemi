// Web side of the Hemi integration. Unity calls window.HemiDigUp (via Plugins/WebGL/HemiBridge.jslib);
// results go back to the "HemiBridge" GameObject with SendMessage. Only window.ethereum is needed:
// the Worker (onchain/worker) builds the EIP-712 payload and relays the signed run, so the player pays no gas.
(function () {
  var API = window.HEMI_API_BASE || ""; // same origin when served by the Cloudflare Worker
  var config = null;

  function send(method, data) {
    if (window.unityInstance) window.unityInstance.SendMessage("HemiBridge", method, JSON.stringify(data));
  }

  function getJson(url, options) {
    return fetch(API + url, options).then(function (r) { return r.json(); });
  }

  function getConfig() {
    if (config) return Promise.resolve(config);
    return getJson("/api/config").then(function (c) { config = c; return c; });
  }

  function errorText(e) {
    if (!e) return "Unknown error";
    if (e.code === 4001) return "Signature cancelled.";
    return e.shortMessage || e.message || String(e);
  }

  async function ensureWallet() {
    var eth = window.ethereum;
    if (!eth) throw new Error("No wallet found. Install MetaMask (or any EVM wallet) to save onchain.");
    var cfg = await getConfig();
    var accounts = await eth.request({ method: "eth_requestAccounts" });
    var chainId = "0x" + cfg.chainId.toString(16);
    try {
      await eth.request({ method: "wallet_switchEthereumChain", params: [{ chainId: chainId }] });
    } catch (e) {
      var code = e && (e.code || (e.data && e.data.originalError && e.data.originalError.code));
      if (code !== 4902) throw e;
      await eth.request({
        method: "wallet_addEthereumChain",
        params: [{
          chainId: chainId,
          chainName: cfg.chainName,
          nativeCurrency: { name: "Ether", symbol: "ETH", decimals: 18 },
          rpcUrls: [cfg.rpcUrl],
          blockExplorerUrls: [cfg.explorer],
        }],
      });
    }
    return accounts[0];
  }

  window.HemiDigUp = {
    submitRun: async function (score, escaped) {
      try {
        send("OnSubmitStatus", { status: "Connecting wallet..." });
        var player = await ensureWallet();

        var typed = await getJson("/api/typed-data?player=" + player + "&score=" + score + "&escaped=" + escaped);
        if (typed.error) throw new Error(typed.error);

        send("OnSubmitStatus", { status: "Sign the message in your wallet (free, no gas)..." });
        var signature = await window.ethereum.request({
          method: "eth_signTypedData_v4",
          params: [player, JSON.stringify(typed)],
        });

        send("OnSubmitStatus", { status: "Saving your run on Hemi..." });
        var res = await getJson("/api/relay", {
          method: "POST",
          headers: { "content-type": "application/json" },
          body: JSON.stringify({
            player: player,
            score: score,
            escaped: escaped,
            deadline: typed.message.deadline,
            signature: signature,
          }),
        });
        send("OnSubmitResult", {
          ok: !!res.ok,
          error: res.error || "",
          txHash: res.txHash || "",
          badge: !!res.badge,
        });
      } catch (e) {
        send("OnSubmitResult", { ok: false, error: errorText(e), txHash: "", badge: false });
      }
    },

    fetchLeaderboard: async function () {
      try {
        var r = await getJson("/api/top");
        send("OnLeaderboard", {
          ok: !r.error,
          entries: (r.entries || []).map(function (e) { return { player: e.player, score: Number(e.score) }; }),
          totalRuns: Number(r.totalRuns || 0),
          totalEscapes: Number(r.totalEscapes || 0),
        });
      } catch (e) {
        send("OnLeaderboard", { ok: false, entries: [], totalRuns: 0, totalEscapes: 0 });
      }
    },
  };
})();
