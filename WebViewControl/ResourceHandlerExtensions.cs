using System;

namespace WebViewControl {

    public static class ResourceHandlerExtensions {

        public static Action<ResourceHandler, Uri>? ExternalLoader;

        public static void LoadAppResource(this ResourceHandler resourceHandler, Uri url) {
            if (ExternalLoader is not null) {
                ExternalLoader(resourceHandler, url);
                return;
            }

            throw new Exception();
        }

        public static void LoadEmbeddedResource(this ResourceHandler resourceHandler, Uri url) {
            if (ExternalLoader is not null) {
                ExternalLoader(resourceHandler, url);
                return;
            }

            var stream = ResourcesManager.TryGetResource(url, true, out string extension);

            if (stream != null) {
                resourceHandler.RespondWith(stream, extension);
            }
        }
    }
}
