﻿using Core.Misc;
using Google.Protobuf;
using Shared;
using Shared.Net;
using System.Collections.Generic;

namespace LoginServer.Net
{
	public class BalanceSession : SrvCliSession
	{
		private readonly List<OneBsInfo> m_BS_List = new List<OneBsInfo>();

		protected BalanceSession( uint id ) : base( id )
		{
			this._msgHandler.Register( ( int )BSToLS.MsgID.EMsgToLsfromBsAskRegister, this.MsgInitHandler );
			this._msgHandler.Register( ( int )BSToLS.MsgID.EMsgToLsfromBcOneClinetLoginCheck, this.MsgHandleOneClientLoginCheck );
		}

		protected override void SendInitData()
		{
			LSToBS.AskRegisterRet sMsg = new LSToBS.AskRegisterRet();
			this.owner.SendMsgToSession( this.id, sMsg, ( int )LSToBS.MsgID.EMsgToBsfromLsAskRegisterRet );
		}

		protected override void OnRealEstablish()
		{
			Logger.Info( $"BS({this.logicID}) Connected." );
		}

		protected override void OnClose()
		{
			Logger.Info( $"BS({this.logicID}) DisConnected." );
		}

		private bool MsgInitHandler( byte[] data, int offset, int size, int msgid )
		{
			this.SetInited( true, true );
			return true;
		}

		/// <summary>
		/// BS请求验证登陆的用户是否合法
		/// </summary>
		private bool MsgHandleOneClientLoginCheck( byte[] data, int offset, int size, int msgid )
		{
			GCToBS.OneClinetLogin oneClientLogin = new GCToBS.OneClinetLogin();
			oneClientLogin.MergeFrom( data, offset, size );

			string sessionid = string.Empty;
			if ( oneClientLogin.Plat == ( uint )EUserPlatform.ePlatform_PC )
			{
				sessionid += oneClientLogin.Plat;
				sessionid += oneClientLogin.Uin;
			}
			else
				sessionid = oneClientLogin.Sessionid;

			LoginUserInfo loginUserInfo = LS.instance.sdkConnector.GetLoginUserInfo( sessionid );
			if ( loginUserInfo != null )
			{
				oneClientLogin.LoginSuccess = 1;
				oneClientLogin.Uin = loginUserInfo.uin;
				LS.instance.sdkConnector.RemoveLoginUserInfo( sessionid );
				Logger.Log( $"vaild uid:{oneClientLogin.Uin}" );
			}
			else
			{
				Logger.Warn( $"fail! user with uin({oneClientLogin.Uin}) not found." );
				oneClientLogin.LoginSuccess = 0;
			}
			//回应BS该登陆的用户是否合法
			this.owner.SendMsgToSession( this.id, oneClientLogin, ( int )LSToBS.MsgID.EMsgToBsfromLsOneClinetLoginCheckRet );
			return true;
		}

		protected override bool HandleUnhandledMsg( byte[] data, int offset, int size, int msgID )
		{
			return true;
		}
	}
}