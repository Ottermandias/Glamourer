using Luna;

namespace Glamourer.Gui.Tabs.DesignTab;

public sealed class DesignHeader : SplitButtonHeader
{
    public DesignHeader(DesignSelection selection, IncognitoButton incognito)
    {
        RightButtons.AddButton(incognito,                   50);
        RightButtons.AddButton(new LockedButton(selection), 100);
    }

    private sealed class LockedButton(DesignSelection selection) : BaseIconButton<AwesomeIcon>
    {
        public override bool IsVisible
            => selection.Design is not null;

        public override AwesomeIcon Icon
            => selection.Design!.WriteProtected() ? LunaStyle.LockedIcon : LunaStyle.UnlockedIcon;
    }
}
