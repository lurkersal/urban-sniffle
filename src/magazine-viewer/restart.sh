#!/bin/bash
pkill -f "dotnet.*MagazineViewer"
sleep 1
cd /home/justin/repos/magazine/magazine-viewer/src && dotnet run > /tmp/dotnet-run.log 2>&1 &
