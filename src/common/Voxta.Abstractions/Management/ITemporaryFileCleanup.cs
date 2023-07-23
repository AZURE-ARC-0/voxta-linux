namespace Voxta.Abstractions.Management;

public interface ITemporaryFileCleanup
{
    void MarkForDeletion(string filename, bool reusable);
}