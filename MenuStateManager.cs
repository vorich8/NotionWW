namespace TeamManagerBot.Handlers
{
    public class MenuStateManager
    {
        private readonly Dictionary<long, int> _messageIds = new(); // chatId -> messageId
        private readonly Dictionary<long, string> _currentMenu = new(); // chatId -> menuType

        public void SetMenuMessage(long chatId, int messageId, string menuType)
        {
            _messageIds[chatId] = messageId;
            _currentMenu[chatId] = menuType;
        }

        public bool TryGetMenuMessage(long chatId, out int messageId, out string menuType)
        {
            if (_messageIds.TryGetValue(chatId, out messageId) &&
                _currentMenu.TryGetValue(chatId, out menuType))
            {
                return true;
            }

            menuType = string.Empty;
            return false;
        }

        public void ClearMenu(long chatId)
        {
            _messageIds.Remove(chatId);
            _currentMenu.Remove(chatId);
        }
    }
}