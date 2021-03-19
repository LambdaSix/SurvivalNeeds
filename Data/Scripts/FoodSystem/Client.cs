using System;
using System.Text;
using DailyNeeds.API.HUDText;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Library.Utils;
using VRage.Utils;
using VRageMath;
// For MyGameModeEnum

namespace DailyNeeds
{
    public static class IconStringRefs
    {
        public static readonly MyStringId ThirstIcon = MyStringId.GetOrCompute("ThirstIcon_LightBlue");
        public static readonly MyStringId ThirstIconRed = MyStringId.GetOrCompute("ThirstIcon_Red");
        public static readonly MyStringId ThirstIconGreen = MyStringId.GetOrCompute("ThirstIcon_Green");
        public static readonly MyStringId HungerIcon = MyStringId.GetOrCompute("HungerIcon_LightBlue");
        public static readonly MyStringId HungerIconRed = MyStringId.GetOrCompute("HungerIcon_Red");
        public static readonly MyStringId HungerIconGreen = MyStringId.GetOrCompute("HungerIcon_Green");
        public static readonly MyStringId FatigueIcon = MyStringId.GetOrCompute("FatigueIcon_LightBlue");
        public static readonly MyStringId FatigueIconRed = MyStringId.GetOrCompute("FatigueIcon_Red");
        public static readonly MyStringId FatigueIconGreen = MyStringId.GetOrCompute("FatigueIcon_Green");
        public static readonly MyStringId TwentyFivePercentHudIconFull = MyStringId.GetOrCompute("25PercentFull");
        public static readonly MyStringId TwentyFivePercentHudIconRed = MyStringId.GetOrCompute("25PercentProgressBarRed");
        public static readonly MyStringId FiftyPercentHudIconHudIconFull = MyStringId.GetOrCompute("50PercentFull");
        public static readonly MyStringId FiftyPercentProgressBarAmber = MyStringId.GetOrCompute("50PercentProgressBarAmber");
        public static readonly MyStringId SeventyFivePercentHudIconFull = MyStringId.GetOrCompute("75PercentFull");
        public static readonly MyStringId OneHundredPercentHudIconFull = MyStringId.GetOrCompute("100PercentFull");
    }

    public struct BillboardInfo
    {
        public HudAPIv2.HUDMessage Message;
        public StringBuilder StringBuilder;

        public HudAPIv2.BillBoardHUDMessage Icon;
        public HudAPIv2.BillBoardHUDMessage OneHundred;
        public HudAPIv2.BillBoardHUDMessage SeventyFive;
        public HudAPIv2.BillBoardHUDMessage Fifty;
        public HudAPIv2.BillBoardHUDMessage TwentyFive;
    }

    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
	public class Client : MySessionComponentBase
	{
		private bool _mStarted = false;
	    private bool _eventsReady = false;
	    private bool _textHudInitialised = false;
        
        private IMyHudNotification _notify = null;
		private PlayerData _playerData = null;

	    private HudAPIv2 _textAPI;

        private BillboardInfo _hungerBillboard;
        private BillboardInfo _thirstBillboard;
        private BillboardInfo _fatigueBillboard;

        private void Init()
		{
		    _textAPI = new HudAPIv2();

            if (Utils.isDev())
			{
				MyAPIGateway.Utilities.ShowMessage("CLIENT", "INIT");
			}

			MyAPIGateway.Utilities.MessageEntered += OnMessageEntered;
			MyAPIGateway.Multiplayer.RegisterMessageHandler(1337, FoodUpdateMsgHandler);
        }

	    public override void UpdateAfterSimulation()
	    {
	        if (MyAPIGateway.Session == null)
	            return;

	        try
            {
                if (GameLogic.SkipProcessing())
                    return;

                if (!_mStarted)
	            {
	                _mStarted = true;
	                Init();
	            }
            }

            catch (Exception e)
	        {
	            Logging.Instance.WriteLine(("(FoodSystem) UpdateSimulation Error: " + e.Message + "\n" + e.StackTrace));
	        }
	    }

        private void ShowNotification(string text)
		{
			if (_notify == null)
			{
				_notify = MyAPIGateway.Utilities.CreateNotification(text, 10000, MyFontEnum.Red);
			}
			else
			{
				_notify.Text = text;
				_notify.ResetAliveTime();
			}

			_notify.Show();
		}

	    private void FoodUpdateMsgHandler(byte[] data)
	    {
	        try
	        {
	            _playerData = MyAPIGateway.Utilities.SerializeFromXML<PlayerData>(Encoding.Unicode.GetString(data));
                var thirstLevel = Math.Floor(_playerData.thirst);
                var hungerLevel = Math.Floor(_playerData.hunger);

                Logging.Instance.WriteLine($"{_playerData} Loaded to Client");
                
                if (_playerData != null && _textAPI.Heartbeat)
	            {
                    if (_playerData.thirst <= 1 && _playerData.hunger <= 1)
                    {
                        ShowNotification($"Warning: You are Thirsty ({thirstLevel}%) and Hungry ({hungerLevel}%)");
                    }
                    else if (_playerData.thirst <= 1)
                    {
                        ShowNotification($"Warning: You are Thirsty ({thirstLevel}%)");
                    }
                    else if (_playerData.hunger <= 1)
                    {
                        ShowNotification($"Warning: You are Hungry ({hungerLevel}%)");
                    }

                    if (!_textHudInitialised)
                    {
                        _textHudInitialised = true;

                        _hungerBillboard = InitHUDHunger();
                        _thirstBillboard = InitHUDThirst();
                        _fatigueBillboard = InitHUDFatigue();
                    } else {
                        UpdateHUD_Hunger();
                        UpdateHUD_Thirst();
                        UpdateHUD_Fatigue();
                    }
                }
            }
	        catch (Exception e)
	        {
	            Logging.Instance.WriteLine(("(FoodSystem) FoodUpdateMsg Error: " + e.Message + "\n" + e.StackTrace));
            }
        }

        public void UpdateBillboard(float percentage, BillboardInfo info)
        {
            if (percentage > 75)
            {
                info.OneHundred.Material = IconStringRefs.OneHundredPercentHudIconFull;
                info.TwentyFive.Material = IconStringRefs.TwentyFivePercentHudIconFull;
                info.Fifty.Material = IconStringRefs.FiftyPercentHudIconHudIconFull;
                info.SeventyFive.Material = IconStringRefs.SeventyFivePercentHudIconFull;

                info.TwentyFive.Visible = true;
                info.Fifty.Visible = true;
                info.SeventyFive.Visible = true;
                info.OneHundred.Visible = true;
            }

            if (percentage < 76)
            {
                info.TwentyFive.Material = IconStringRefs.TwentyFivePercentHudIconFull;
                info.Fifty.Material = IconStringRefs.FiftyPercentHudIconHudIconFull;
                info.SeventyFive.Material = IconStringRefs.SeventyFivePercentHudIconFull;

                info.TwentyFive.Visible = true;
                info.Fifty.Visible = true;
                info.SeventyFive.Visible = true;
                info.OneHundred.Visible = false;

            }
            if (percentage < 51 && percentage > 25)
            {
                info.TwentyFive.Material = IconStringRefs.TwentyFivePercentHudIconRed;
                info.Fifty.Material = IconStringRefs.FiftyPercentProgressBarAmber;

                info.TwentyFive.Visible = true;
                info.Fifty.Visible = true;
                info.SeventyFive.Visible = false;
                info.OneHundred.Visible = false;
            }

            if (percentage < 26 && percentage > 0)
            {
                info.TwentyFive.Material = IconStringRefs.TwentyFivePercentHudIconRed;

                info.TwentyFive.Visible = true;
                info.Fifty.Visible = false;
                info.SeventyFive.Visible = false;
                info.OneHundred.Visible = false;
            }

            if (percentage <= 0)
            {
                info.TwentyFive.Visible = info.Fifty.Visible = info.SeventyFive.Visible = info.OneHundred.Visible = false;
            }
        }

        public void UpdateHudIcon(float percentage, BillboardInfo billboardInfo, params MyStringId[] levels)
        {
            if (percentage > 100)
            {
                billboardInfo.Icon.Material = levels[0];
            }

            if (percentage > 0 && percentage <= 100)
            {
                billboardInfo.Icon.Material = levels[1];
            }

            if (percentage <= 0)
            {
                billboardInfo.Icon.Material = levels[2];
            }
        }

        public void UpdateHudText(float percentage, StringBuilder stringBuilder)
        {
            stringBuilder.Clear();
            if (percentage <= 125 && percentage > 30)
                stringBuilder.Append($"<color=white>{percentage}");

            else if (percentage <= 30 && percentage > 5)
                stringBuilder.Append($"<color=yellow>{percentage}");

            else if (percentage <= 5)
                stringBuilder.Append($"<color=red>{percentage}");
        }

        private void UpdateHud(float statLevel, BillboardInfo billboard,
            params MyStringId[] iconRefs)
        {
            UpdateHudText(statLevel, billboard.StringBuilder);
            UpdateHudIcon(statLevel, billboard, iconRefs);
            UpdateBillboard(statLevel, billboard);
        }

        private void UpdateHUD_Fatigue()
        {
            UpdateHud((float)Math.Floor(_playerData.fatigue), _fatigueBillboard,
                IconStringRefs.FatigueIconGreen, IconStringRefs.FatigueIcon, IconStringRefs.FatigueIconRed);
        }

        private void UpdateHUD_Thirst()
        {
            UpdateHud((float) Math.Floor(_playerData.thirst), _thirstBillboard,
                IconStringRefs.ThirstIconGreen, IconStringRefs.ThirstIcon, IconStringRefs.ThirstIconRed);
        }

        private void UpdateHUD_Hunger()
        {
            UpdateHud((float)Math.Floor(_playerData.hunger), _hungerBillboard,
                IconStringRefs.HungerIconGreen, IconStringRefs.HungerIcon, IconStringRefs.HungerIconRed);
        }

        private BillboardInfo InitHUDFatigue()
        {
            StringBuilder fatigueStringBuilder = new StringBuilder();
            var fatigueHudMessage = new HudAPIv2.HUDMessage(fatigueStringBuilder,
                new Vector2D(-0.766f, -0.505f), Scale: 1.2d);

            var fatigueIconBillboardMessage = new HudAPIv2.BillBoardHUDMessage(
                    IconStringRefs.FatigueIcon,
                    new Vector2D(-0.941, -0.515), Color.White)
                {Height = 1.55f, Scale = 0.02d, Rotation = 0f};

            var fatigueBar25BillboardMessage = new HudAPIv2.BillBoardHUDMessage(
                    IconStringRefs.TwentyFivePercentHudIconFull,
                    new Vector2D(-0.906f, -0.52), Color.White)
                {Height = 2.0f, Rotation = 0f, Scale = 0.07f};

            var fatigueBar50BillboardMessage = new HudAPIv2.BillBoardHUDMessage(
                    IconStringRefs.FiftyPercentHudIconHudIconFull,
                    new Vector2D(-0.868f, -0.52), Color.White)
                {Height = 1.85f, Scale = 0.07f, Rotation = 0f};

            var fatigueBar75BillboardMessage = new HudAPIv2.BillBoardHUDMessage(
                    IconStringRefs.SeventyFivePercentHudIconFull,
                    new Vector2D(-0.8295f, -0.52), Color.White)
                {Height = 1.85f, Scale = 0.07f, Rotation = 0f};

            var fatigueBar100BillboardMessage = new HudAPIv2.BillBoardHUDMessage(
                    IconStringRefs.OneHundredPercentHudIconFull,
                    new Vector2D(-0.791f, -0.52), Color.White)
                {Height = 1.85f, Rotation = 0f, Scale = 0.07f};

            return new BillboardInfo
            {
                Message = fatigueHudMessage,
                StringBuilder = fatigueStringBuilder,
                Icon = fatigueIconBillboardMessage,
                TwentyFive = fatigueBar25BillboardMessage,
                Fifty = fatigueBar50BillboardMessage,
                SeventyFive = fatigueBar75BillboardMessage,
                OneHundred = fatigueBar100BillboardMessage
            };
        }

        private BillboardInfo InitHUDThirst()
        {
            StringBuilder thirstStringBuilder = new StringBuilder();
            var thirstHudMessage = new HudAPIv2.HUDMessage(thirstStringBuilder, new Vector2D(-0.766f, -0.445f), Scale: 1.2d);

            var thirstIconBillboardMessage = new HudAPIv2.BillBoardHUDMessage(
                    IconStringRefs.ThirstIcon,
                    new Vector2D(-0.941, -0.460), Color.White)
                {Height = 1.55f, Rotation = 0f, Scale = 0.02d};

            var thirstBar25BillboardMessage = new HudAPIv2.BillBoardHUDMessage(
                    IconStringRefs.TwentyFivePercentHudIconFull,
                    new Vector2D(-0.906f, -0.465), Color.White)
                {Height = 1.85f, Scale = 0.07f, Rotation = 0f};

            var thirstBar50BillboardMessage = new HudAPIv2.BillBoardHUDMessage(
                    IconStringRefs.FiftyPercentHudIconHudIconFull,
                    new Vector2D(-0.868f, -0.465), Color.White)
                {Height = 1.85f, Scale = 0.07f, Rotation = 0f};

            var thirstBar75BillboardMessage = new HudAPIv2.BillBoardHUDMessage(
                    IconStringRefs.SeventyFivePercentHudIconFull,
                    new Vector2D(-0.8295f, -0.465), Color.White)
                {Height = 1.85f, Scale = 0.07f, Rotation = 0f};

            var thirstBar100BillboardMessage = new HudAPIv2.BillBoardHUDMessage(
                    IconStringRefs.OneHundredPercentHudIconFull,
                    new Vector2D(-0.791f, -0.465), Color.White)
                {Height = 1.85f, Scale = 0.07f, Rotation = 0f};

            return new BillboardInfo
            {
                Message = thirstHudMessage,
                StringBuilder = thirstStringBuilder,
                Icon = thirstIconBillboardMessage,
                TwentyFive = thirstBar25BillboardMessage,
                Fifty = thirstBar50BillboardMessage,
                SeventyFive = thirstBar75BillboardMessage,
                OneHundred = thirstBar100BillboardMessage
            };
        }

        private BillboardInfo InitHUDHunger()
        {
            StringBuilder hungerStringBuilder = new StringBuilder();
            var hungerHudMessage = new HudAPIv2.HUDMessage(hungerStringBuilder, new Vector2D(-0.766f, -0.4f), Scale: 1.2d);

            var hungerIconBillboardMessage = new HudAPIv2.BillBoardHUDMessage(
                    IconStringRefs.HungerIcon,
                    new Vector2D(-0.941, -0.405), Color.White)
                {Height = 1.65f, Scale = 0.02d, Rotation = 0f};

            var hungerBar25BillboardMessage = new HudAPIv2.BillBoardHUDMessage(
                    IconStringRefs.TwentyFivePercentHudIconFull,
                    new Vector2D(-0.906f, -0.41), Color.White)
                {Height = 1.85f, Scale = 0.07f, Rotation = 0f};

            var hungerBar50BillboardMessage = new HudAPIv2.BillBoardHUDMessage(
                    IconStringRefs.FiftyPercentHudIconHudIconFull,
                    new Vector2D(-0.868f, -0.41), Color.White)
                {Height = 1.85f, Scale = 0.07f, Rotation = 0f};

            var hungerBar75BillboardMessage = new HudAPIv2.BillBoardHUDMessage(
                    IconStringRefs.SeventyFivePercentHudIconFull,
                    new Vector2D(-0.8295f, -0.41), Color.White)
                {Height = 1.85f, Scale = 0.07f, Rotation = 0f};

            var hungerBar100BillboardMessage = new HudAPIv2.BillBoardHUDMessage(
                    IconStringRefs.OneHundredPercentHudIconFull,
                    new Vector2D(-0.791f, -0.41), Color.White)
                {Height = 1.85f, Scale = 0.07f, Rotation = 0f};

            return new BillboardInfo
            {
                Message = hungerHudMessage,
                StringBuilder = hungerStringBuilder,
                Icon = hungerIconBillboardMessage,
                TwentyFive = hungerBar25BillboardMessage,
                Fifty = hungerBar50BillboardMessage,
                SeventyFive = hungerBar75BillboardMessage,
                OneHundred = hungerBar100BillboardMessage
            };
        }

        private void OnMessageEntered(string messageText, ref bool sendToOthers)
        {
            sendToOthers = true;

            if (!messageText.StartsWith("/")) return;

            var words = messageText.Trim().ToLower().Replace("/", "").Split(' ');

            if (words.Length > 0)
            {
                switch (words[0])
                {
                    case "food":
                        var thirstLevel = Math.Floor(_playerData.thirst);
                        var hungerLevel = Math.Floor(_playerData.hunger);
                        var fatigueLevel = Math.Floor(_playerData.fatigue);

                        if (MyAPIGateway.Session.SessionSettings.GameMode == MyGameModeEnum.Creative)
                        {
                            MyAPIGateway.Utilities.ShowMessage("FoodSystem",
                                "Hunger, thirst and fatigue are disabled in creative mode.");
                        }
                        else if (_playerData != null)
                        {
                            if (_playerData.fatigue < 9000)
                            {
                                if (words.Length > 1 && words[1] == "detail")
                                    MyAPIGateway.Utilities.ShowMessage("FoodSystem",
                                        $"Hunger: {_playerData.hunger}% - Thirst: {_playerData.thirst}% - Fatigue: {_playerData.fatigue}%");
                                else
                                    MyAPIGateway.Utilities.ShowMessage("FoodSystem",
                                        $"Hunger: {hungerLevel}% - Thirst: {thirstLevel}% - Fatigue: {fatigueLevel}%");
                            }
                            else
                            {
                                if (words.Length > 1 && words[1] == "detail")
                                    MyAPIGateway.Utilities.ShowMessage("FoodSystem",
                                        $"Hunger: {_playerData.hunger}% - Thirst: {_playerData.thirst}%");
                                else
                                    MyAPIGateway.Utilities.ShowMessage("FoodSystem",
                                        $"Hunger: {hungerLevel}% - Thirst: {thirstLevel}%");
                            }
                        }
                        break;

                    case "needs":
                        sendToOthers = false;
                        if (words.Length > 1)
                        {
                            Command cmd = new Command(MyAPIGateway.Multiplayer.MyId, words[1]);

                            string message = MyAPIGateway.Utilities.SerializeToXML<Command>(cmd);
                            MyAPIGateway.Multiplayer.SendMessageToServer(
                                1338,
                                Encoding.Unicode.GetBytes(message)
                            );
                        }
                        break;
                }
            }
        }

        protected override void UnloadData() // will act up without the try-catches. yes it's ugly and slow. it only gets called on disconnect so we don't care
		{
		    try
		    {
		        try
		        {
		            _textAPI.Close();
		        }
		        catch (Exception e)
		        {
		            MyAPIGateway.Utilities.ShowMessage("Error", e.Message + "\n" + e.StackTrace);
		        }

		        ;
		        try
		        {
		            _mStarted = false;
		        }
		        catch (Exception e)
		        {
		            MyAPIGateway.Utilities.ShowMessage("Error", e.Message + "\n" + e.StackTrace);
		        }

		        ;
		        try
		        {
		            MyAPIGateway.Multiplayer.UnregisterMessageHandler(1337, FoodUpdateMsgHandler);
		        }
		        catch (Exception e)
		        {
		            MyAPIGateway.Utilities.ShowMessage("Error", e.Message + "\n" + e.StackTrace);
		        }

		        ;
		        try
		        {
		            MyAPIGateway.Utilities.MessageEntered -= OnMessageEntered;
		        }
		        catch (Exception e)
		        {
		            MyAPIGateway.Utilities.ShowMessage("Error", e.Message + "\n" + e.StackTrace);
		        }

		        ;
		    }
		    catch (Exception e)
		    {
		        Logging.Instance.WriteLine(("(FoodSystem) Client Unload Data Error: " + e.Message + "\n" + e.StackTrace));
            };
		}
	}
}
 