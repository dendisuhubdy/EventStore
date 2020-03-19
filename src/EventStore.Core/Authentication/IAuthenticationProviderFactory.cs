﻿using System;

namespace EventStore.Core.Authentication {
	public interface IAuthenticationProviderFactory {
		IAuthenticationProvider BuildAuthenticationProvider(bool logFailedAuthenticationAttempts, Action onStarted);
	}
}
