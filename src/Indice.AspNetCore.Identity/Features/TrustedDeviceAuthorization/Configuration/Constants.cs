﻿using System;

namespace Indice.AspNetCore.Identity.Features
{
    internal static class TrustedDeviceRegistrationRequest
    {
        public const string Mode = "mode";
        public const string DeviceId = "device_id";

        public static InteractionMode? GetInteractionMode(string mode) {
            if (string.IsNullOrWhiteSpace(mode)) {
                return default;
            }
            if (mode.Equals("fingerprint", StringComparison.OrdinalIgnoreCase)) {
                return InteractionMode.Fingerprint;
            }
            if (mode.Equals("4pin", StringComparison.OrdinalIgnoreCase)) {
                return InteractionMode.FourPin;
            }
            return default;
        }
    }
}
