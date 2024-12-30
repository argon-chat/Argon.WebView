using Xilium.CefGlue;

namespace WebViewControl {

    public partial class ChromiumBrowser {

        public CefBrowser GetBrowser() {
            return UnderlyingBrowser;
        }
    }
}
