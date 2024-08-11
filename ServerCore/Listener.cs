using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ServerCore
{
	public class Listener
	{
		Socket _listenSocket;
		
		// seession의 대한 작업을 코어단이 아닌 외부에서 작업을 진행해야 하기 때문에,
		// // 코어와 외부를 연결해줄 징검다리가 필요한데, 그 역할을 하는것이 아래 변수 입니다.
		Func<Session> _sessionFactory;

		public void Init(IPEndPoint endPoint, Func<Session> sessionFactory)
		{
			_listenSocket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
			_sessionFactory += sessionFactory;

			// 문지기 교육
			_listenSocket.Bind(endPoint);

			// 영업 시작
			// backlog : 최대 대기수
			_listenSocket.Listen(10);

			// 비동기 accept를 진행하기 위해 아래 작업이 필요하다.
			// 전체적인 흐름을 보면 c++의  wsarecv 처럼 일단 예약을 걸어 넣고 성공 되면 그때 진행하는 방식입니다.
			// 얘내들은 별도의 작업자 스레드에서 돌고 있습니다.
			SocketAsyncEventArgs args = new SocketAsyncEventArgs();
			args.Completed += new EventHandler<SocketAsyncEventArgs>(OnAcceptCompleted);
			RegisterAccept(args);
		}

		void RegisterAccept(SocketAsyncEventArgs args) 
		{
			args.AcceptSocket = null; // 처음 등록할때는 해당 변수를 반드시 초기화 진행해주세요

			bool pending = _listenSocket.AcceptAsync(args); // 해당 함수로 accept를 비동기로 예약을 걸어 놓습니다. 
															// 만일 예약을 걸자마자 성공을 했다면 false 값이 리턴이 됩니다..
			if (pending == false)
            {
				OnAcceptCompleted(null, args); // accept가 성공했다를 의미 합니다.
			}
				
		}

		void OnAcceptCompleted(object sender, SocketAsyncEventArgs args) 
		{
			if (args.SocketError == SocketError.Success) //SocketError 에 성공도 포함하기 때문에 따로 작업해줬습니다.
			{
				Session session = _sessionFactory.Invoke();
				session.Start(args.AcceptSocket);
				session.OnConnected(args.AcceptSocket.RemoteEndPoint);
			}
			else
			{
				Console.WriteLine(args.SocketError.ToString());
			}
			
			//accept의 대한 작업이 완료 되었기 때문에 다시 예약을 걸어줘야 합니다.
			RegisterAccept(args);
		}
	}
}
