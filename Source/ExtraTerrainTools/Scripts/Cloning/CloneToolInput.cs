namespace TerrainTools.Cloning
{
    public record CloneToolInput
    {
        public string DescriptionKey { get; init; }
        public string KeybindId { get; init; }
    }

    public static class CloneToolInputExtensions
    {
        public static bool HasKeybind(this CloneToolInput action)
        {
            return action.KeybindId != "";
        }
        public static bool HasDescription(this CloneToolInput action)
        {
            return action.DescriptionKey != "";
        }
    }
}