#!/bin/bash
set -e
set -x


echo "🔥🔥🔥 Startup.sh is running 🔥🔥🔥"  


echo "Current user: $(whoami)"
echo "Current directory: $(pwd)"

# Start CUPS in the background
/usr/sbin/cupsd

# Wait for CUPS to be ready
sleep 2

# Add printer if not already present
if ! lpstat -p | grep -q "myprinter"; then
  lpadmin -p myprinter -E -v "$PrinterUrl/ipp/print" -m everywhere
fi
echo "test"

# Set default printer
lpoptions -d myprinter

# Show printers for debugging
lpstat -a

# Start your .NET app
# exec dotnet XmppBot.dll
