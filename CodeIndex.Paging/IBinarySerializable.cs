using System.IO;

namespace CodeIndex.Paging
{
    public interface IBinarySerializable
    {
        void Serialize(BinaryWriter writer, int rowSize);

        void Deserialize(BinaryReader reader, int rowSize);
    }
}