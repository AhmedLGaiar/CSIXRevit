namespace ExportJsonFileFromRevit
{
    public interface IDataExporter<T>
    {
        void Export(T data, string filePath);
    }
}
