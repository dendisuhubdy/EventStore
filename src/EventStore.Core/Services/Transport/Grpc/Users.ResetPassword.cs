using System.Threading.Tasks;
using EventStore.Core.Messages;
using EventStore.Core.Messaging;
using EventStore.Client.Users;
using Grpc.Core;

namespace EventStore.Core.Services.Transport.Grpc {
	public partial class Users {
		public override async Task<ResetPasswordResp> ResetPassword(ResetPasswordReq request,
			ServerCallContext context) {
			var options = request.Options;

			var user = context.GetHttpContext().User;

			var resetPasswordSource = new TaskCompletionSource<bool>();

			var envelope = new CallbackEnvelope(OnMessage);

			_queue.Publish(
				new UserManagementMessage.ResetPassword(envelope, user, options.LoginName, options.NewPassword));

			await resetPasswordSource.Task.ConfigureAwait(false);

			return new ResetPasswordResp();

			void OnMessage(Message message) {
				if (HandleErrors(options.LoginName, message, resetPasswordSource)) return;

				resetPasswordSource.TrySetResult(true);
			}
		}
	}
}
