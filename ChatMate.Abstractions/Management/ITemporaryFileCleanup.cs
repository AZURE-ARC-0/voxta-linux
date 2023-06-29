namespace ChatMate.Abstractions.Management;

public interface ITemporaryFileCleanup
{
    void MarkForDeletion(string filename);
}