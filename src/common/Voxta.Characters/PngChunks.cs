using System.Text;

namespace Voxta.Characters;

public static class PngChunkReader
{
    private static readonly byte[] PNGHeader = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

    public static async Task<List<PngChunk>> ExtractPngChunksAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        const int headerLength = 8;
        var header = new byte[headerLength];
        if (await stream.ReadAsync(header.AsMemory(0, headerLength), cancellationToken) < headerLength || !IsPngHeader(header))
            throw new ArgumentException("The provided stream does not represent a valid PNG file.");

        var chunks = new List<PngChunk>();

        while (stream.Position < stream.Length)
        {
            // Each chunk is structured as:
            //  - 4 bytes: the unsigned integer length of the data
            //  - 4 bytes: the chunk type/name
            //  - the data itself
            //  - 4 bytes: CRC-32 checksum

            var lengthBytes = new byte[4];
            var typeBytes = new byte[4];

            if (await stream.ReadAsync(lengthBytes.AsMemory(0, 4), cancellationToken) < 4 || await stream.ReadAsync(typeBytes.AsMemory(0, 4), cancellationToken) < 4) // Not enough data for another chunk
                break;

            var length = BitConverter.ToInt32(lengthBytes.Reverse().ToArray(), 0); // PNG uses network byte order (big-endian)
            var type = Encoding.ASCII.GetString(typeBytes);

            if (stream.Length - stream.Position < length)
                break;

            var data = new byte[length];
            if(await stream.ReadAsync(data.AsMemory(0, length), cancellationToken) < headerLength)
                break;

            chunks.Add(new PngChunk { Type = type, Data = data });
            stream.Seek(4, SeekOrigin.Current); // Skip CRC
        }

        return chunks;
    }

    private static bool IsPngHeader(IEnumerable<byte> data)
    {
        
        return data.Take(8).SequenceEqual(PNGHeader);
    }
    
    public static TextChunk Decode(PngChunk chunk)
    {
        if (chunk == null)
            throw new ArgumentNullException(nameof(chunk));
        if (chunk.Type != "tEXt")
            throw new ArgumentException("Chunk is not a tEXt chunk.", nameof(chunk));

        // A tEXt chunk contains:
        // - null-terminated keyword (1-79 bytes)
        // - text data (remaining bytes)

        var nullPosition = Array.IndexOf(chunk.Data, (byte)0);
        if (nullPosition < 0)
            throw new FormatException("tEXt chunk does not contain a null-terminated keyword.");

        var keyword = Encoding.ASCII.GetString(chunk.Data, 0, nullPosition);
        var text = Encoding.ASCII.GetString(chunk.Data, nullPosition + 1, chunk.Data.Length - nullPosition - 1);

        return new TextChunk { Type = chunk.Type, Data = chunk.Data, Keyword = keyword, Text = text };
    }

}

public class PngChunk
{
    public required string Type { get; init; }
    public required byte[] Data { get; init; }
}

public class TextChunk : PngChunk
{
    public required string Keyword { get; init; }
    public required string Text { get; init; }
}