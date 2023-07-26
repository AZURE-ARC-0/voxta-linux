namespace Voxta.Abstractions.Exceptions;

public class ServiceDisabledException : Exception
{
    public ServiceDisabledException() : base("Service disabled")
    {
    }
}