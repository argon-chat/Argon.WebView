using System;
using Xilium.CefGlue;

namespace WebViewControl {

    public interface ICefExternalHandlerLoader {
        CefResourceHandler Load(WebView ownerWebView, CefBrowser browser, CefFrame frame, CefRequest request);
    }
    public static class CefResourceHandlerLockup {
        public static ICefExternalHandlerLoader Loader;
    }

    partial class WebView {

        private class InternalResourceRequestHandler : CefResourceRequestHandler {

            public InternalResourceRequestHandler(WebView ownerWebView) {
                OwnerWebView = ownerWebView;
            }

            private WebView OwnerWebView { get; }

            protected override CefCookieAccessFilter GetCookieAccessFilter(CefBrowser browser, CefFrame frame, CefRequest request) {
                return null;
            }

            protected override CefResourceHandler GetResourceHandler(CefBrowser browser, CefFrame frame, CefRequest request) {
                if (request.Url == OwnerWebView.DefaultLocalUrl) {
                    return AsyncResourceHandler.FromText(OwnerWebView.htmlToLoad  ?? "");
                }

                if (request.Url.StartsWith("https://index") && CefResourceHandlerLockup.Loader is not null) {
                    return CefResourceHandlerLockup.Loader.Load(OwnerWebView, browser, frame, request);
                }

                if (UrlHelper.IsChromeInternalUrl(request.Url)) {
                    return null;
                }

                var resourceHandler = new ResourceHandler(request, OwnerWebView.GetRequestUrl(request.Url, (ResourceType)request.ResourceType));

                void TriggerBeforeResourceLoadEvent() {
                    var beforeResourceLoad = OwnerWebView.BeforeResourceLoad;
                    if (beforeResourceLoad != null) {
                        OwnerWebView.ExecuteWithAsyncErrorHandling(() => beforeResourceLoad(resourceHandler));
                    }
                }

                if (Uri.TryCreate(resourceHandler.Url, UriKind.Absolute, out var url) && url.Scheme == ResourceUrl.EmbeddedScheme) {
                    resourceHandler.BeginAsyncResponse(() => {
                        var urlWithoutQuery = new UriBuilder(url);
                        if (!string.IsNullOrEmpty(url.Query)) {
                            urlWithoutQuery.Query = string.Empty;
                        }

                        OwnerWebView.ExecuteWithAsyncErrorHandling(() => resourceHandler.LoadEmbeddedResource(urlWithoutQuery.Uri));

                        TriggerBeforeResourceLoadEvent();

                        if (resourceHandler.Handled || OwnerWebView.IgnoreMissingResources) {
                            return;
                        }

                        var resourceLoadFailed = OwnerWebView.ResourceLoadFailed;
                        if (resourceLoadFailed != null) {
                            resourceLoadFailed(url.ToString());
                        } else {
                            OwnerWebView.ForwardUnhandledAsyncException(new InvalidOperationException("Resource not found: " + url));
                        }
                    });
                } else if (Uri.TryCreate(resourceHandler.Url, UriKind.Absolute, out var urlApp) && urlApp.Scheme == ResourceUrl.AppScheme) {
                    resourceHandler.BeginAsyncResponse(() => {
                        var urlWithoutQuery = new UriBuilder(urlApp);
                        if (!string.IsNullOrEmpty(urlApp.Query)) {
                            urlWithoutQuery.Query = string.Empty;
                        }

                        OwnerWebView.ExecuteWithAsyncErrorHandling(() => resourceHandler.LoadEmbeddedResource(urlWithoutQuery.Uri));

                        TriggerBeforeResourceLoadEvent();

                        if (resourceHandler.Handled || OwnerWebView.IgnoreMissingResources) {
                            return;
                        }

                        var resourceLoadFailed = OwnerWebView.ResourceLoadFailed;
                        if (resourceLoadFailed != null) {
                            resourceLoadFailed(urlApp.ToString());
                        } else {
                            OwnerWebView.ForwardUnhandledAsyncException(new InvalidOperationException("Resource not found: " + urlApp));
                        }
                    });
                } else {
                    TriggerBeforeResourceLoadEvent();
                }

                return resourceHandler.Handler;
            }
        }
    }
}
