using Dalamud.Interface.Textures;
using Dalamud.Plugin.Services;
using OtterGui.Classes;
using OtterGui.Services;
using Penumbra.GameData.Enums;
using Race = Penumbra.GameData.Enums.Race;

namespace Glamourer.GameData;

/// <summary> Generate everything about customization per tribe and gender. </summary>
public class CustomizeManager : IAsyncDataContainer
{
    /// <summary> All races except for Unknown </summary>
    public static readonly IReadOnlyList<Race> Races = ((Race[])Enum.GetValues(typeof(Race))).Skip(1).ToArray();

    /// <summary> All tribes except for Unknown </summary>
    public static readonly IReadOnlyList<SubRace> Clans = ((SubRace[])Enum.GetValues(typeof(SubRace))).Skip(1).ToArray();

    /// <summary> Two genders. </summary>
    public static readonly IReadOnlyList<Gender> Genders =
    [
        Gender.Male,
        Gender.Female,
    ];

    /// <summary> Every tribe and gender has a separate set of available customizations. </summary>
    public CustomizeSet GetSet(SubRace race, Gender gender)
    {
        if (!Finished)
            Awaiter.Wait();
        return _customizationSets[ToIndex(race, gender)];
    }

    /// <summary> Get specific icons. </summary>
    public ISharedImmediateTexture GetIcon(uint id)
        => _icons.TextureProvider.GetFromGameIcon(id);

    /// <summary> Iterate over all supported genders and clans. </summary>
    public static IEnumerable<(SubRace Clan, Gender Gender)> AllSets()
    {
        foreach (var clan in Clans)
        {
            yield return (clan, Gender.Male);
            yield return (clan, Gender.Female);
        }
    }

    public CustomizeManager(ITextureProvider textures, IDataManager gameData, IPluginLog log, NpcCustomizeSet npcCustomizeSet)
    {
        _icons = new TextureCache(gameData, textures);
        var stopwatch = new Stopwatch();
        var tmpTask = Task.Run(() =>
        {
            stopwatch.Start();
            return new CustomizeSetFactory(gameData, log, _icons, npcCustomizeSet);
        });
        var setTasks = AllSets().Select(p
            => tmpTask.ContinueWith(t => _customizationSets[ToIndex(p.Clan, p.Gender)] = t.Result.CreateSet(p.Clan, p.Gender)));
        Awaiter = Task.WhenAll(setTasks).ContinueWith(_ =>
        {
            // This is far too hard to estimate sensibly.
            TotalCount = 0;
            Memory     = 0;
            Time       = stopwatch.ElapsedMilliseconds;
        });
    }

    /// <inheritdoc/>
    public Task Awaiter { get; }

    /// <inheritdoc/>
    public bool Finished
        => Awaiter.IsCompletedSuccessfully;

    private readonly        TextureCache   _icons;
    private static readonly int            ListSize           = Clans.Count * Genders.Count;
    private readonly        CustomizeSet[] _customizationSets = new CustomizeSet[ListSize];

    /// <summary> Get the index for the given pair of tribe and gender. </summary>
    private static int ToIndex(SubRace race, Gender gender)
    {
        var idx = ((int)race - 1) * Genders.Count + (gender == Gender.Female ? 1 : 0);
        if (idx < 0 || idx >= ListSize)
            throw new Exception($"Invalid customization requested for {race} {gender}.");

        return idx;
    }

    public long Time   { get; private set; }
    public long Memory { get; private set; }

    public string Name
        => nameof(CustomizeManager);

    public int TotalCount { get; private set; }
}
