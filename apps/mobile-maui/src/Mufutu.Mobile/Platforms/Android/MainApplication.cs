using Android.App;
using Android.Runtime;

namespace Mufutu.Mobile;

[Application]
public class MainApplication : MauiApplication
{
    public MainApplication(IntPtr handle, JniHandleOwnership ownership)
        : base(handle, ownership)
    {
        AndroidEnvironment.UnhandledExceptionRaiser += (_, args) =>
            CrashLog.Write("AndroidEnvironment", args.Exception);

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            CrashLog.Write("AppDomain", args.ExceptionObject as Exception);

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            CrashLog.Write("TaskScheduler", args.Exception);
            args.SetObserved();
        };
    }

    protected override MauiApp CreateMauiApp()
    {
        try
        {
            return MauiProgram.CreateMauiApp();
        }
        catch (Exception ex)
        {
            CrashLog.Write("CreateMauiApp", ex);
            throw;
        }
    }
}
