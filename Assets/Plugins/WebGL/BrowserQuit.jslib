mergeInto(LibraryManager.library, {
  TryCloseBrowserWindow: function () {
    if (typeof window === "undefined") {
      return;
    }

    try {
      window.close();
    } catch (e) {
      // Browser may block closing tabs not opened by script.
    }

    // Fallback for browsers that block window.close().
    if (window.history && window.history.length > 1) {
      try {
        window.history.back();
      } catch (e) {
        // Ignore navigation fallback failure.
      }
    }
  }
});
