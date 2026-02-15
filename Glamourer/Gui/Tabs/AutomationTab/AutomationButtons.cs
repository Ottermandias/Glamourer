using Glamourer.Automation;
using ImSharp;
using Luna;
using Penumbra.GameData.Actors;
using Penumbra.GameData.Interop;
using Penumbra.GameData.Structs;
using Penumbra.String;

namespace Glamourer.Gui.Tabs.AutomationTab;

public sealed class AutomationButtons : ButtonFooter
{
    public AutomationButtons(Configuration config, AutoDesignManager manager, AutomationSelection selection, ActorObjectManager objects)
    {
        Buttons.AddButton(new AddButton(objects, manager),              100);
        Buttons.AddButton(new DuplicateButton(selection, manager),      90);
        Buttons.AddButton(new HelpButton(),                             80);
        Buttons.AddButton(new DeleteButton(selection, config, manager), 70);
    }

    private sealed class AddButton(ActorObjectManager objects, AutoDesignManager manager) : BaseIconButton<AwesomeIcon>
    {
        private ActorIdentifier _identifier;

        public override AwesomeIcon Icon
            => LunaStyle.AddObjectIcon;

        public override bool HasTooltip
            => true;

        protected override void PreDraw()
        {
            _identifier = objects.Actors.GetCurrentPlayer();
            if (!_identifier.IsValid)
                _identifier = objects.Actors.CreatePlayer(ByteString.FromSpanUnsafe("New Design"u8, true, false, true), WorldId.AnyWorld);
        }

        public override void DrawTooltip()
            => Im.Text($"Create a new Automatic Design Set for {_identifier}. The associated player can be changed later.");

        public override bool Enabled
            => _identifier.IsValid;

        public override void OnClick()
            => manager.AddDesignSet("New Automation Set", _identifier);
    }

    private sealed class DuplicateButton(AutomationSelection selection, AutoDesignManager manager) : BaseIconButton<AwesomeIcon>
    {
        public override AwesomeIcon Icon
            => LunaStyle.DuplicateIcon;

        public override bool HasTooltip
            => true;

        public override void DrawTooltip()
            => Im.Text("Duplicate the current Automatic Design Set."u8);

        public override bool Enabled
            => selection.Set is not null;

        public override void OnClick()
            => manager.DuplicateDesignSet(selection.Set!);
    }

    private sealed class HelpButton : BaseIconButton<AwesomeIcon>
    {
        public override AwesomeIcon Icon
            => LunaStyle.InfoIcon;

        public override bool HasTooltip
            => true;

        public override void DrawTooltip()
            => Im.Text("How does Automation work?"u8);


        public override void OnClick()
            => Im.Popup.Open("Automation Help"u8);

        protected override void PostDraw()
        {
            var longestLine =
                Im.Font.CalculateSize(
                    "A single set can contain multiple automated designs that apply under different conditions and different parts of their design."u8);
            ImEx.HelpPopup("Automation Help"u8, new Vector2(longestLine.X + 50 * Im.Style.GlobalScale, 33 * Im.Style.TextHeightWithSpacing),
                DrawHelp);
        }

        private static void DrawHelp()
        {
            var halfLine = new Vector2(Im.Style.TextHeight / 2);
            Im.Dummy(halfLine);
            Im.Text("What is Automation?"u8);
            Im.BulletText("Automation helps you to automatically apply Designs to specific characters under specific circumstances."u8);
            Im.Dummy(halfLine);

            Im.Text("Automated Design Sets"u8);
            Im.BulletText("First, you create automated design sets. An automated design set can be... "u8);
            using var indent = Im.Indent();
            Im.BulletText("... enabled, or"u8, ColorId.EnabledAutoSet.Value());
            Im.BulletText("... disabled."u8,   ColorId.DisabledAutoSet.Value());
            indent.Unindent();
            Im.BulletText("You can create new, empty automated design sets, or duplicate existing ones."u8);
            Im.BulletText("You can name automated design sets arbitrarily."u8);
            Im.BulletText("You can re-order automated design sets via drag & drop in the selector."u8);
            Im.BulletText("Each automated design set is assigned to exactly one specific character."u8);
            indent.Indent();
            Im.BulletText("On creation, it is assigned to your current Player Character."u8);
            Im.BulletText("You can assign sets to any players, retainers, mannequins and most human NPCs."u8);
            Im.BulletText("Only one automated design set can be enabled at the same time for each specific character."u8);
            indent.Indent();
            Im.BulletText("Enabling another automatically disables the prior one."u8);
            indent.Unindent();
            indent.Unindent();

            Im.Dummy(halfLine);
            Im.Text("Automated Designs"u8);
            Im.BulletText(
                "A single set can contain multiple automated designs that apply under different conditions and different parts of their design."u8);
            Im.BulletText("The order of these automated designs can also be changed via drag & drop, and is relevant for the application."u8);
            Im.BulletText("Automated designs respect their own, coarse applications rules, and the designs own application rules."u8);
            Im.BulletText("Automated designs can be configured to be job- or job-group specific and only apply on these jobs, then."u8);
            Im.BulletText("There is also the special option 'Reset', which can be used to reset remaining slots to the game's values."u8);
            Im.BulletText("Automated designs apply from top to bottom, either on top of your characters current state, or its game state."u8);
            Im.BulletText("For a value to apply, it needs to:"u8);
            indent.Unindent();
            Im.BulletText("Be configured to apply in the design itself."u8);
            Im.BulletText("Be configured to apply in the automation rules."u8);
            Im.BulletText("Fulfill the conditions of the automation rules."u8);
            Im.BulletText("Be a valid value for the current (on its own application) state of the character."u8);
            Im.BulletText("Not have had anything applied to the same value before from a different design."u8);
            indent.Unindent();
        }
    }

    private sealed class DeleteButton(AutomationSelection selection, Configuration config, AutoDesignManager manager)
        : BaseIconButton<AwesomeIcon>
    {
        private bool _enabled;

        public override AwesomeIcon Icon
            => LunaStyle.DeleteIcon;

        public override bool HasTooltip
            => true;

        public override void DrawTooltip()
        {
            if (selection.Set is null)
            {
                Im.Text("No Automatic Design Set selected."u8);
            }
            else
            {
                Im.Text("Delete the currently selected design set."u8);
                if (!_enabled)
                    Im.Text($"\nHold {config.DeleteDesignModifier} to delete.");
            }
        }

        public override bool Enabled
            => (_enabled = config.DeleteDesignModifier.IsActive()) && selection.Set is not null;

        public override void OnClick()
            => manager.DeleteDesignSet(selection.Index);
    }
}
