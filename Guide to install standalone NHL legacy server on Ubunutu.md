# Zamboni NHL14 Legacy Server Setup Guide (Ubuntu VPS)

This guide installs and configures the Zamboni NHL14 legacy server stack
on a **single Ubuntu VPS**.
After installing in RPC3 configuration IP/Host Switches change gosredirector.ea.com==95.217.209.57  to gosredirector.ea.com==Your VPS IP
## Important

Replace the following values everywhere in this guide:

-   `11.11.11.11` → your VPS IP address
-   `CHANGE_ME_STRONG_PASSWORD` → your PostgreSQL password=
 ------------------------------------------------------------------------

# Clean Install

## Connect to the VPS

``` bash
ssh root@11.11.11.11
```

## Stop and remove old services

``` bash
systemctl stop zamboni14.service 2>/dev/null || true
systemctl stop qos.service 2>/dev/null || true

systemctl disable zamboni14.service 2>/dev/null || true
systemctl disable qos.service 2>/dev/null || true

rm -f /etc/systemd/system/zamboni14.service
rm -f /etc/systemd/system/qos.service
systemctl daemon-reload
```

## Remove old app files and user

``` bash
rm -rf /opt/zamboni
userdel -r zamboni 2>/dev/null || true
```

## Reset PostgreSQL

``` bash
sudo -u postgres psql -c "DROP DATABASE IF EXISTS zamboni;"
sudo -u postgres psql -c "DROP ROLE IF EXISTS zamboni;"
```

------------------------------------------------------------------------

# Install Base Packages

``` bash
apt update && apt upgrade -y
apt install -y git curl wget gnupg ca-certificates ufw postgresql postgresql-contrib apt-transport-https
```

------------------------------------------------------------------------

# Install .NET 8 SDK

## Ubuntu 24.04

``` bash
wget https://packages.microsoft.com/config/ubuntu/24.04/packages-microsoft-prod.deb
dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

apt update
apt install -y dotnet-sdk-8.0
```

Verify installation:

``` bash
dotnet --list-sdks
dotnet --list-runtimes
```

------------------------------------------------------------------------

# Create Service User and Folders

``` bash
useradd -r -m -d /opt/zamboni -s /bin/bash zamboni

mkdir -p /opt/zamboni/publish
chown -R zamboni:zamboni /opt/zamboni
```

------------------------------------------------------------------------

# Clone Repositories

``` bash
sudo -u zamboni -H bash -lc '
cd /opt/zamboni
git clone --branch master https://github.com/ZamboniDevelopment/Zamboni14Legacy.git
git clone --branch nhl14legacy-compatability https://github.com/ZamboniDevelopment/BlazeSDK.git
git clone --branch master https://github.com/Aim4kill/PSN.git
git clone --branch nhl10-compatability-lazy https://github.com/ZamboniDevelopment/Skateboard3Server.Qos.git
'
```

------------------------------------------------------------------------

# Create PostgreSQL Database

``` bash
sudo -u postgres psql -c "CREATE USER zamboni WITH PASSWORD 'CHANGE_ME_STRONG_PASSWORD';"
sudo -u postgres psql -c "CREATE DATABASE zamboni OWNER zamboni;"
```

------------------------------------------------------------------------

# Upload TLS Certificate

Change ":\Users\E\Downloads\" to the ceritificate path
And run **this on your Windows PC**, not on the VPS:

``` bash
scp "C:\Users\E\Downloads\gosredirector_mod.pfx" root@11.11.11.11:/opt/zamboni/Zamboni14Legacy/gosredirector_mod.pfx
```

Fix permissions on the VPS:

``` bash
chown zamboni:zamboni /opt/zamboni/Zamboni14Legacy/gosredirector_mod.pfx
chmod 600 /opt/zamboni/Zamboni14Legacy/gosredirector_mod.pfx
```

------------------------------------------------------------------------

# Create Zamboni Config

``` bash
sudo -u zamboni tee /opt/zamboni/Zamboni14Legacy/zamboni-config.yml > /dev/null <<'EOF'
GameServerIp: "11.11.11.11"
GameServerPort: 16767
LogLevel: "Info"
DatabaseConnectionString: "Host=localhost;Port=5432;Username=zamboni;Password=CHANGE_ME_STRONG_PASSWORD;Database=zamboni"
HostRedirectorInstance: true
ApiServerIdentifier: "nhl14"
ApiServerPort: "8082"
EOF
```

------------------------------------------------------------------------

# Create QoS Config

``` bash
cat > /opt/zamboni/Skateboard3Server.Qos/src/Skateboard3Server.Qos/appsettings.json <<'EOF'
{
  "Logging": {
    "LogLevel": {
      "Default": "Trace",
      "Microsoft": "Information",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "AllowedHosts": "*",
  "Qos": {
    "QosIp": "11.11.11.11",
    "FirewallPrimaryIp": "11.11.11.11",
    "FirewallSecondaryIp": "11.11.11.11"
  }
}
EOF
```

------------------------------------------------------------------------

# OpenSSL Compatibility Config

``` bash
cat > /opt/zamboni/openssl-zamboni.cnf <<'EOF'
openssl_conf = openssl_init

[openssl_init]
providers = provider_sect
alg_section = algorithm_sect
ssl_conf = ssl_sect

[provider_sect]
default = default_sect
legacy = legacy_sect

[default_sect]
activate = 1

[legacy_sect]
activate = 1

[algorithm_sect]

[ssl_sect]
system_default = system_default_sect

[system_default_sect]
CipherString = DEFAULT:@SECLEVEL=0
MinProtocol = TLSv1
EOF
```

------------------------------------------------------------------------

# Enable and Start Services

``` bash
systemctl daemon-reload

systemctl enable postgresql
systemctl enable zamboni14.service
systemctl enable qos.service

systemctl start postgresql
systemctl start zamboni14.service
systemctl start qos.service
```

------------------------------------------------------------------------

# Configure Firewall

``` bash
ufw allow OpenSSH
ufw allow 42127/tcp
ufw allow 16767/tcp
ufw allow 8082/tcp
ufw allow 17502/tcp

ufw allow 17499/udp
ufw allow 17500/udp
ufw allow 17501/udp

ufw --force enable
ufw status
```

------------------------------------------------------------------------

# Verify Installation

``` bash
systemctl status zamboni14.service --no-pager
systemctl status qos.service --no-pager
systemctl status postgresql --no-pager
```

Check ports:

``` bash
ss -ltnup | grep -E '42127|16767|8082|17502|17499|17500|17501|5432'
```

Test API:

``` bash
curl http://127.0.0.1:8082/nhl14/status
curl -v http://127.0.0.1:17502/
```

View logs:

``` bash
journalctl -u zamboni14.service -n 50 --no-pager
journalctl -u qos.service -n 50 --no-pager
```

------------------------------------------------------------------------

# Updating the Server

``` bash
sudo -u zamboni -H bash -lc '
cd /opt/zamboni/Zamboni14Legacy && git pull
cd /opt/zamboni/BlazeSDK && git pull
cd /opt/zamboni/PSN && git pull
cd /opt/zamboni/Skateboard3Server.Qos && git pull
'
```

Republish:

``` bash
sudo -u zamboni -H bash -lc '
export DOTNET_CLI_HOME=/opt/zamboni/.dotnet
export NUGET_PACKAGES=/opt/zamboni/.nuget/packages

cd /opt/zamboni/Zamboni14Legacy
dotnet publish -c Release -o /opt/zamboni/publish/z14

cd /opt/zamboni/Skateboard3Server.Qos/src/Skateboard3Server.Qos
dotnet publish -c Release -r linux-x64 --self-contained true -o /opt/zamboni/publish/qos
'
```

Restart:

``` bash
systemctl restart zamboni14.service
systemctl restart qos.service
```

------------------------------------------------------------------------

# Clean Uninstall

Stop services:

``` bash
systemctl stop zamboni14.service 2>/dev/null || true
systemctl stop qos.service 2>/dev/null || true

systemctl disable zamboni14.service 2>/dev/null || true
systemctl disable qos.service 2>/dev/null || true
```

Remove services:

``` bash
rm -f /etc/systemd/system/zamboni14.service
rm -f /etc/systemd/system/qos.service
systemctl daemon-reload
```

Remove app:

``` bash
rm -rf /opt/zamboni
userdel -r zamboni 2>/dev/null || true
```

Remove database:

``` bash
sudo -u postgres psql -c "DROP DATABASE IF EXISTS zamboni;"
sudo -u postgres psql -c "DROP ROLE IF EXISTS zamboni;"
```

Remove firewall rules (optional):

``` bash
ufw delete allow 42127/tcp
ufw delete allow 16767/tcp
ufw delete allow 8082/tcp
ufw delete allow 17502/tcp
ufw delete allow 17499/udp
ufw delete allow 17500/udp
ufw delete allow 17501/udp

ufw status
```

------------------------------------------------------------------------

# Final Notes

Replace:

-   `11.11.11.11` with your VPS IP
-   `CHANGE_ME_STRONG_PASSWORD` with your PostgreSQL password
