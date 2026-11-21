using System.Threading.Tasks;
public static class ExportTrailer
{
    public static Task<string> Main()=>Hakaton.Demo.Trailer.Editor.TrailerFrames.Export("/tmp/hakaton-trailer-franks-choice-frames",24);
}
