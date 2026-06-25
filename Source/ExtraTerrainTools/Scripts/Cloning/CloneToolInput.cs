using System.Collections.Generic;

namespace TerrainTools.Cloning
{
    public enum InputTextSource
    {
        Localization,
        Keybind
    }

    public record InputText(string Id, InputTextSource Source) { }

    public record CloneToolInput(InputText Text, params InputText[] Arguments)
    {
        public class Builder
        {
            private InputText _text;
            private List<InputText> _arguments = new();

            public Builder Text(string id) { _text = In.Text(id); return this; }
            public Builder Keybind(string id) { _text = In.Keybind(id); return this; }
            public Builder Format(string id) { _text = In.Text(id); return this; }

            public Builder WithText(string id) { _arguments.Add(In.Keybind(id)); return this; }
            public Builder WithKeybind(string id) { _arguments.Add(In.Keybind(id)); return this; }

            internal CloneToolInput Build() => new(_text, _arguments.ToArray());
        }
    }

    public static class In
    {
        public static InputText Text(string id) => new(id, InputTextSource.Localization);
        public static InputText Keybind(string id) => new(id, InputTextSource.Keybind);
        public static CloneToolInput TextInput(string textId) => new(Text(textId));
        public static CloneToolInput KeyInput(string keybindId) => new(Keybind(keybindId));
        public static CloneToolInput FormattedInput(string formatId, params InputText[] inputs) => new(Text(formatId), inputs);
    }
}