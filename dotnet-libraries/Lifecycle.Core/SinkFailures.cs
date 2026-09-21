namespace Lifecycle.Core;

internal static class SinkFailures
{
    private static bool IsOrdinary(Exception error)
    {

        return error is not OutOfMemoryException and not StackOverflowException and not AccessViolationException;
    }

    internal static void Emit(ILifecycleEventSink sink, LifecycleEvent lifecycleEvent, Action<Exception> onError)
    {

        try
        {
            sink.Emit(lifecycleEvent);
        }
        catch (Exception error) when (IsOrdinary(error))
        {
            try
            {
                onError(error);
            }
            catch (Exception callbackError) when (IsOrdinary(callbackError))
            {
                // Diagnostic callbacks must not make telemetry failures affect the application.
            }
        }
    }
}
