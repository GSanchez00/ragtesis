using Microsoft.Extensions.VectorData;

public class ChunkVectorRecord
{
    [VectorStoreKey]
    public Guid Id { get; set; }

    [VectorStoreData]
    public string Libro { get; set; } = "";

    [VectorStoreData]
    public string Seccion { get; set; } = "";

    [VectorStoreData]
    public string Texto { get; set; } = "";

    [VectorStoreVector(EmbeddingConfig.Dimensiones, DistanceFunction = DistanceFunction.CosineSimilarity)]
    public ReadOnlyMemory<float> Vector { get; set; }
}