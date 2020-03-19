using EventStore.Core.Messaging;

namespace EventStore.Core.Authentication {
	public class AuthenticationMessage {
		public sealed class AuthenticationProviderInitialized : Message {
			private static readonly int TypeId = System.Threading.Interlocked.Increment(ref NextMsgId);

			public override int MsgTypeId {
				get { return TypeId; }
			}
		}
	}
}
