using System;

namespace Lilly.Physics.Plugin.Services;

internal sealed record SubscriptionInfo(int BodyId, Action Handler);
