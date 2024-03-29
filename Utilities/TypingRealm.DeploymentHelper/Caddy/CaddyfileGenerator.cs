﻿using System.Linq;
using System.Text;
using TypingRealm.DeploymentHelper.Data;

namespace TypingRealm.DeploymentHelper.Caddy;

public sealed class CaddyfileGenerator
{
    public string GenerateCaddyfile(DeploymentData data, CaddyProfile profile)
    {
        var sb = new StringBuilder();
        if (profile.SpecifyEmail)
        {
            sb.AppendLine("{");
            sb.AppendLine($"    email {Constants.Email}");
            sb.AppendLine("}");
        }

        void GenerateSection(string prefix, CaddyProfile caddyProfile, string serviceNamePrefix)
        {
            var domainPrefix = string.IsNullOrEmpty(prefix) ? string.Empty : $"{prefix}.";
            var servicePrefix = string.IsNullOrEmpty(prefix) ? string.Empty : $"{prefix}-";

            sb.AppendLine();
            sb.AppendLine($"{domainPrefix}{caddyProfile.Domain} {{");
            sb.AppendLine($"    reverse_proxy {servicePrefix}{caddyProfile.WebUiAddress}");
            sb.AppendLine("}");

            sb.AppendLine();
            sb.AppendLine($"{domainPrefix}api.{caddyProfile.Domain} {{");

            foreach (var service in data.Services
                .Where(s => s.AddToReverseProxyInProduction || !caddyProfile.IsProd)
                .OrderBy(service => service.ServiceName))
            {
                sb.AppendLine($"    handle_path /{service.ServiceName}/* {{");
                sb.AppendLine($"        reverse_proxy {servicePrefix}{Constants.GetReverseProxyAddressWithPort(service, serviceNamePrefix)}");
                sb.AppendLine("    }");
                sb.AppendLine();
            }

            sb.AppendLine("    respond 404");
            sb.AppendLine("}");
        }

        GenerateSection(string.Empty, profile, profile.Value == "local" ? "local-" : string.Empty);

        if (profile.Value == "host")
        {
            GenerateSection("dev", profile, string.Empty);
            GenerateSection(string.Empty, new CaddyProfile("local"), "local-");
        }

        return sb.ToString();
    }
}
