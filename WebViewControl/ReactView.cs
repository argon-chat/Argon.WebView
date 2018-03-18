﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace WebViewControl {

    public partial class ReactView : ContentControl, IReactView {

        private static Window window;
        private static ReactViewRender cachedView;

        static ReactView() {
            EventManager.RegisterClassHandler(typeof(Window), Window.LoadedEvent, new RoutedEventHandler(OnWindowLoaded), true);
        }

        private static void OnWindowLoaded(object sender, EventArgs e) {
            var window = (Window)sender;
            window.Closed -= OnWindowLoaded;
            window.Closed += OnWindowClosed;
        }

        private static void OnWindowClosed(object sender, EventArgs e) {
            var windows = Application.Current.Windows.Cast<Window>();
            if (Debugger.IsAttached) {
                windows = windows.Where(w => w.GetType().FullName != "Microsoft.VisualStudio.DesignTools.WpfTap.WpfVisualTreeService.Adorners.AdornerLayerWindow");
            }
            if (windows.Count() == 1 && windows.Single() == window) {
                // close helper window
                window.Close();
                window = null;
            }
        }

        private static ReactViewRender CreateReactViewInstance() {
            var result = cachedView;
            cachedView = null;
            Application.Current.Dispatcher.BeginInvoke((Action)(() => {
                if (cachedView == null && !Application.Current.Dispatcher.HasShutdownStarted) {
                    cachedView = new ReactViewRender();
                    if (window == null) {
                        window = new Window() {
                            ShowActivated = false,
                            WindowStyle = WindowStyle.None,
                            ShowInTaskbar = false,
                            Visibility = Visibility.Hidden,
                            Width = 50,
                            Height = 50,
                            Top = int.MinValue,
                            Left = int.MinValue,
                            IsEnabled = false,
                            Title = "ReactViewRender Background Window"
                        };
                        window.Show();
                    }
                    window.Content = cachedView;
                }
            }), DispatcherPriority.Background);
            return result ?? new ReactViewRender();
        }

        private readonly ReactViewRender view;

        public ReactView(bool usePreloadedWebView = true) {
            if (usePreloadedWebView) {
                view = CreateReactViewInstance();
            } else {
                view = new ReactViewRender();
            }
            SetResourceReference(StyleProperty, typeof(ReactView)); // force styles to be inherited, must be called after view is created otherwise view might be null
            Content = view;
            Dispatcher.BeginInvoke(DispatcherPriority.Send, (Action)(() => {
                Initialize();
            }));
        }

        private void Initialize() {
            view.LoadComponent(JavascriptSource, JavascriptName, CreateNativeObject());
        }

        public static readonly DependencyProperty DefaultStyleSheetProperty = DependencyProperty.Register(
            nameof(DefaultStyleSheet),
            typeof(string),
            typeof(ReactView), 
            new PropertyMetadata(OnDefaultStyleSheetPropertyChanged));

        private static void OnDefaultStyleSheetPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            ((ReactView)d).view.DefaultStyleSheet = (string) e.NewValue;
        }

        public string DefaultStyleSheet {
            get { return (string)GetValue(DefaultStyleSheetProperty); }
            set { SetValue(DefaultStyleSheetProperty, value); }
        }

        public static readonly DependencyProperty ModulesProperty = DependencyProperty.Register(
            nameof(Modules),
            typeof(IViewModule[]),
            typeof(ReactView),
            new PropertyMetadata(OnModulesPropertyChanged));

        private static void OnModulesPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            ((ReactView)d).view.Modules = (IViewModule[])e.NewValue;
        }

        public IViewModule[] Modules {
            get { return (IViewModule[])GetValue(ModulesProperty); }
            set { SetValue(ModulesProperty, value); }
        }

        public bool EnableDebugMode { get => view.EnableDebugMode; set => view.EnableDebugMode = value; }

        public bool IsReady => view.IsReady;

        public event Action Ready {
            add { view.Ready += value; }
            remove { view.Ready -= value; }
        }

        public event Action<UnhandledExceptionEventArgs> UnhandledAsyncException {
            add { view.UnhandledAsyncException += value; }
            remove { view.UnhandledAsyncException -= value; }
        }

        public void Dispose() {
            view.Dispose();
        }

        protected virtual string JavascriptSource => null;

        protected virtual string JavascriptName => null;

        protected virtual object CreateNativeObject() {
            return null;
        }

        protected void ExecuteMethodOnRoot(string methodCall, params string[] args) {
            view.ExecuteMethodOnRoot(methodCall, args);
        }

        protected T EvaluateMethodOnRoot<T>(string methodCall, params string[] args) {
            return view.EvaluateMethodOnRoot<T>(methodCall, args);
        }

        public void ShowDeveloperTools() {
            view.ShowDeveloperTools();
        }

        public void CloseDeveloperTools() {
            view.CloseDeveloperTools();
        }
    }
}
