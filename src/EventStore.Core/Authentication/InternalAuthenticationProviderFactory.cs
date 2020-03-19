using EventStore.Core.Bus;
using EventStore.Core.Helpers;
using EventStore.Core.Messages;
using EventStore.Core.Messaging;
using EventStore.Core.Services;
using EventStore.Core.Services.Transport.Http;
using EventStore.Core.Services.Transport.Http.Authentication;
using EventStore.Core.Services.Transport.Http.Controllers;
using EventStore.Core.Services.UserManagement;
using EventStore.Core.Settings;

namespace EventStore.Core.Authentication {
	public class InternalAuthenticationProviderFactory : IAuthenticationProviderFactory {
		private readonly IPublisher _mainQueue;
		private readonly ISubscriber _mainBus;
		private readonly IPublisher _workersQueue;
		private readonly InMemoryBus[] _workerBuses;

		public InternalAuthenticationProviderFactory(IPublisher mainQueue, ISubscriber mainBus,
			IPublisher workersQueue, InMemoryBus[] workerBuses) {
			_mainQueue = mainQueue;
			_mainBus = mainBus;
			_workersQueue = workersQueue;
			_workerBuses = workerBuses;
		}
		public IAuthenticationProvider BuildAuthenticationProvider(bool logFailedAuthenticationAttempts) {
			var passwordHashAlgorithm = new Rfc2898PasswordHashAlgorithm();
			var dispatcher = new IODispatcher(_mainQueue, new PublishEnvelope(_workersQueue, crossThread: true));

			foreach (var bus in _workerBuses) {
				bus.Subscribe(dispatcher.ForwardReader);
				bus.Subscribe(dispatcher.BackwardReader);
				bus.Subscribe(dispatcher.Writer);
				bus.Subscribe(dispatcher.StreamDeleter);
				bus.Subscribe(dispatcher.Awaker);
				bus.Subscribe(dispatcher);
			}

			// USER MANAGEMENT
			var ioDispatcher = new IODispatcher(_mainQueue, new PublishEnvelope(_mainQueue));
			_mainBus.Subscribe(ioDispatcher.BackwardReader);
			_mainBus.Subscribe(ioDispatcher.ForwardReader);
			_mainBus.Subscribe(ioDispatcher.Writer);
			_mainBus.Subscribe(ioDispatcher.StreamDeleter);
			_mainBus.Subscribe(ioDispatcher.Awaker);
			_mainBus.Subscribe(ioDispatcher);

			var userManagement = new UserManagementService(_mainQueue, ioDispatcher, passwordHashAlgorithm,
				skipInitializeStandardUsersCheck: false);
			_mainBus.Subscribe<UserManagementMessage.Create>(userManagement);
			_mainBus.Subscribe<UserManagementMessage.Update>(userManagement);
			_mainBus.Subscribe<UserManagementMessage.Enable>(userManagement);
			_mainBus.Subscribe<UserManagementMessage.Disable>(userManagement);
			_mainBus.Subscribe<UserManagementMessage.Delete>(userManagement);
			_mainBus.Subscribe<UserManagementMessage.ResetPassword>(userManagement);
			_mainBus.Subscribe<UserManagementMessage.ChangePassword>(userManagement);
			_mainBus.Subscribe<UserManagementMessage.Get>(userManagement);
			_mainBus.Subscribe<UserManagementMessage.GetAll>(userManagement);
			_mainBus.Subscribe<SystemMessage.BecomeLeader>(userManagement);
			_mainBus.Subscribe<SystemMessage.BecomeFollower>(userManagement);

			var provider =
				new InternalAuthenticationProvider(dispatcher, passwordHashAlgorithm,
					ESConsts.CachedPrincipalCount, logFailedAuthenticationAttempts);
			var passwordChangeNotificationReader = new PasswordChangeNotificationReader(_mainQueue, dispatcher);
			_mainBus.Subscribe<SystemMessage.SystemStart>(passwordChangeNotificationReader);
			_mainBus.Subscribe<SystemMessage.BecomeShutdown>(passwordChangeNotificationReader);
			_mainBus.Subscribe(provider);
			return provider;
		}

		public void RegisterHttpControllers(IHttpService externalHttpService, HttpSendService httpSendService,
			IPublisher mainQueue, IPublisher networkSendQueue) {
			var usersController = new UsersController(httpSendService, mainQueue, networkSendQueue);
			externalHttpService.SetupController(usersController);
		}
	}
}
