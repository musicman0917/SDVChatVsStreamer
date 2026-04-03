using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace SDVChatVsStreamer;

public static class MrQiDialogue
{
    public static void Init(IModHelper helper) { }

    public static void Show(string[] lines, Action? onDismissed = null)
    {
        // Try real Mr. Qi first (if player has unlocked Ginger Island)
        var qi = Game1.getCharacterFromName("MrQi");
        if (qi != null)
        {
            var dialogueText = string.Join("#", lines);
            var dialogue = new Dialogue(qi, "MrQi_CVS", dialogueText);
            qi.CurrentDialogue.Clear();
            qi.CurrentDialogue.Push(dialogue);
            Game1.drawDialogue(qi);
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