
using Discord;
using Discord.Interactions;

namespace MsgTruck
{
    public class ModalData : IModal
    {
        public string Title => "Load your mail to the truck";
        [InputLabel("End of Message ID or URL")]
        [ModalTextInput("EndId", TextInputStyle.Short, maxLength: 200)]
        [RequiredInput(true)]
        public string EndId { get; set; } = "";
        [InputLabel("Channel Name or ID (ID takes priority)")]
        [ModalTextInput("Channel", TextInputStyle.Short)]
        [RequiredInput(true)]
        public string Channel { get; set; } = "";
    }
}
