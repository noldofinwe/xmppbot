#!/bin/bash

set -e
set -x

echo "Current user: $(whoami)"
echo "Current directory: $(pwd)"
echo "Printer URL: $PrinterUrl"
# Start CUPS in the background
/usr/sbin/cupsd

# Wait for CUPS to be ready
sleep 2

# Add printer if not already present
if ! lpstat -p | grep -q "myprinter"; then
  lpadmin -p myprinter -E -v  $PrinterUrl -m everywhere
fi
echo "test"

# Set default printer
lpoptions -d myprinter

# Show printers for debugging
lpstat -a

# Start your .NET app
exec dotnet XmppBot.dll
