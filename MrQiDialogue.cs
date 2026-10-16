using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace SDVChatVsStreamer;

public static class MrQiDialogue
{
    public static void Init(IModHelper helper) { }

    /// <summary>Shows <paramref name="lines"/> as a real character popup (portrait included)
    /// if <paramref name="characterName"/> resolves to a loaded NPC, falling back to a plain
    /// portrait-less dialogue box otherwise -- including when characterName is left empty,
    /// e.g. for a "message from nobody in particular" line. Originally built just for Mr. Qi
    /// (still the default), now generic enough for any character.</summary>
    public static void Show(string[] lines, Action? onDismissed = null, string characterName = "MrQi")
    {
        var npc = string.IsNullOrEmpty(characterName) ? null : Game1.getCharacterFromName(characterName);
        if (npc != null)
        {
            var dialogueText = string.Join("#", lines);
            var dialogue = new Dialogue(npc, $"{characterName}_CVS", dialogueText);
            npc.CurrentDialogue.Clear();
            npc.CurrentDialogue.Push(dialogue);
            Game1.drawDialogue(npc);
        }
        else
        {
            // Plain dialogue box — shows each line as a separate page
            Game1.activeClickableMenu = new DialogueBox(lines.ToList());
        }

        if (onDismissed != null)
            ModEntry.PendingDismissalAction = onDismissed;
    }
}