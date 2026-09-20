using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.Qdrant;
using Qdrant.Client;

public class VectorStoreSetup
{
    private readonly QdrantVectorStore _vectorStore;
    private readonly VectorStoreCollection<Guid, ChunkVectorRecord> _collection;

    public VectorStoreSetup(string nombreColeccion = "tesis-chunks")
    {
        var qdrantClient = new QdrantClient("localhost", 6334);
        _vectorStore = new QdrantVectorStore(qdrantClient, ownsClient: true);
        _collection = _vectorStore.GetCollection<Guid, ChunkVectorRecord>(nombreColeccion);
    }

    public async Task InicializarAsync()
    {
        await _collection.EnsureCollectionExistsAsync();
    }

    public async Task GuardarChunksAsync(List<(ChunkMetadata Chunk, float[] Vector)> chunksConEmbeddings)
    {
        var registros = chunksConEmbeddings.Select(x => new ChunkVectorRecord
        {
            Id = Guid.NewGuid(),
            Libro = x.Chunk.Libro,
            Seccion = x.Chunk.Seccion,
            Texto = x.Chunk.Texto,
            Vector = x.Vector
        });
        await _collection.UpsertAsync(registros);
    }

    public async Task<List<(ChunkVectorRecord Chunk, double Score)>> BuscarAsync(ReadOnlyMemory<float> vectorConsulta, int top = 5)
    {
        var resultados = new List<(ChunkVectorRecord, double)>();

        await foreach (var resultado in _collection.SearchAsync(vectorConsulta, top))
        {
            resultados.Add((resultado.Record, resultado.Score ?? 0));
        }

        return resultados;
    }

    public async Task<bool> TieneChunksGuardadosAsync()
{
    var conteo = 0;
    await foreach (var _ in _collection.GetAsync(filter: r => true, top: 1))
    {
        conteo++;
    }
    return conteo > 0;
}
}