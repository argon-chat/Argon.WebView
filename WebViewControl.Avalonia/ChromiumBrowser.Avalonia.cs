using System;
using Xilium.CefGlue;
using Xilium.CefGlue.Avalonia;

namespace WebViewControl {

    partial class ChromiumBrowser : AvaloniaCefBrowser {

        public ChromiumBrowser(Func<CefRequestContext> cefRequestContextFactory = null) : base(cefRequestContextFactory) {
            
        }

        public new void CreateBrowser(int width, int height) {
            if (IsBrowserInitialized) {
                return;
            }
            base.CreateBrowser(width, height);
        }
    }
}
