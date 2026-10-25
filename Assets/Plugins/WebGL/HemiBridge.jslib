// Unity -> browser calls for the Hemi integration (implemented in WebGLTemplates/Hemi/TemplateData/hemi.js)
mergeInto(LibraryManager.library, {
  Hemi_IsAvailable: function () {
    return typeof window !== "undefined" && window.HemiDigUp ? 1 : 0;
  },
  Hemi_SubmitRun: function (score, escaped) {
    window.HemiDigUp.submitRun(score, escaped !== 0);
  },
  Hemi_FetchLeaderboard: function () {
    window.HemiDigUp.fetchLeaderboard();
  },
});
