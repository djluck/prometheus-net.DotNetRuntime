namespace Prometheus.DotNetRuntime.EventListening
{
    public interface IEvents
    {
    }

    public interface IInfoEvents : IEvents
    {
    }
    
    public interface IVerboseEvents : IEvents
    {
    }
    
    public interface IErrorEvents : IEvents
    {
    }
    
    public interface ICriticalEvents : IEvents
    {
    }
    
    public interface IWarningEvents : IEvents
    {
    }

    public interface IAlwaysEvents : IEvents
    {
    }

    public interface ICounterEvents : IAlwaysEvents
    {
    }
}