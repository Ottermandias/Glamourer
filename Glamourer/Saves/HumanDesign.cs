using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Glamourer.Customization;
using Newtonsoft.Json.Linq;
using CustomizeData = Penumbra.GameData.Structs.CustomizeData;

namespace Glamourer.Saves;

public partial class Design
{
    private CustomizeData _customizeData;
    public  CustomizeFlag CustomizeFlags { get; private set; }

    public Choice this[CustomizeIndex index]
        => new(this, index);

    public unsafe Customize Customize
        => new((CustomizeData*)Unsafe.AsPointer(ref _customizeData));

    public readonly struct Choice
    {
        private readonly Design         _data;
        private readonly CustomizeFlag  _flag;
        private readonly CustomizeIndex _index;

        public Choice(Design design, CustomizeIndex index)
        {
            _data  = design;
            _index = index;
            _flag  = index.ToFlag();
        }

        public CustomizeValue Value
        {
            get => _data._customizeData.Get(_index);
            set => _data._customizeData.Set(_index, value);
        }

        public bool Apply
        {
            get => _data.CustomizeFlags.HasFlag(_flag);
            set => _data.CustomizeFlags = value ? _data.CustomizeFlags | _flag : _data.CustomizeFlags & ~_flag;
        }

        public CustomizeIndex Index
            => _index;
    }

    public IEnumerable<Choice> Customization
        => Enum.GetValues<CustomizeIndex>().Select(index => new Choice(this, index));


    public IEnumerable<Choice> ActiveCustomizations
        => Customization.Where(c => c.Apply);

    private void WriteCustomization(JObject obj)
    {
        var tok = new JObject();
        foreach (var choice in Customization)
            tok[choice.Index.ToString()] = choice.Value.Value;

        obj[nameof(Customization)] = tok;
    }
}
