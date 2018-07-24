using System;
using System.Globalization;
using System.Reactive;
using System.Reactive.Linq;
using Android.App;
using Android.Content;
using Java.Util;
using Acr;
using Android.Content.PM;
using Android.OS;
using App = Android.App.Application;
using Observable = System.Reactive.Linq.Observable;


namespace Plugin.DeviceInfo
{
    public class AppImpl : IApp
    {
        readonly AppStateLifecyle appState;


        public AppImpl()
        {
            this.appState = new AppStateLifecyle();
            var app = Application.Context.ApplicationContext as Application;
            if (app == null)
                throw new ApplicationException("Invalid application context");

            app.RegisterActivityLifecycleCallbacks(this.appState);
            app.RegisterComponentCallbacks(this.appState);
        }


        public CultureInfo CurrentCulture => this.GetCurrentCulture();


        public IObservable<CultureInfo> WhenCultureChanged() => AndroidObservables
            .WhenIntentReceived(Intent.ActionLocaleChanged)
            .Select(x => this.GetCurrentCulture());


        public IObservable<AppState> WhenStateChanged() => Observable.Create<AppState>(ob =>
        {
            var handler = new EventHandler((sender, args) =>
            {
                var state = this.appState.IsActive ? AppState.Foreground : AppState.Background;
                ob.OnNext(state);
            });
            this.appState.StatusChanged += handler;

            return () => this.appState.StatusChanged -= handler;
        });


        PowerManager.WakeLock wakeLock;
        public bool IsIdleTimerEnabled => this.wakeLock != null;


        public IObservable<Unit> EnableIdleTimer(bool enabled)
        {
            var mgr = (PowerManager)Application.Context.GetSystemService(Context.PowerService);

            if (enabled)
            {
                if (this.wakeLock == null)
                {
                    this.wakeLock = mgr.NewWakeLock(WakeLockFlags.Partial, this.GetType().FullName);
                    this.wakeLock.Acquire();
                }
            }
            else
            {
                this.wakeLock?.Release();
                this.wakeLock = null;
            }

            return Observable.Return(Unit.Default);
        }


        static PackageInfo GetPackage() => App
            .Context
            .ApplicationContext
            .PackageManager
            .GetPackageInfo(App.Context.PackageName, 0);


        public string BundleName => GetPackage().PackageName;
        public string Version => GetPackage().VersionName;
        public string ShortVersion => GetPackage().VersionCode.ToString();


        public bool IsBackgrounded => !this.appState.IsActive;
        //var mgr = (ActivityManager) Application.Context.GetSystemService(Context.ActivityService);
        //var tasks = mgr.GetRunningTasks(Int16.MaxValue);
        //var result = tasks.Any(x => x.TopActivity.PackageName.Equals(App.Context.PackageName));
        //return !result;


        protected virtual CultureInfo GetCurrentCulture()
        {
            var value = Locale.Default.ToString().Replace("_", "-");
            return new CultureInfo(value);
        }


    }
}