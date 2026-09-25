#!/bin/bash

# PUKALE FURNISHING CENTRE - Start Script
# Runs both backend (.NET) and frontend (Angular) simultaneously

ROOT_DIR="$(cd "$(dirname "$0")" && pwd)"
BACKEND_DIR="$ROOT_DIR/backend"
FRONTEND_DIR="$ROOT_DIR/frontend"

echo ""
echo "================================================"
echo "   PUKALE FURNISHING CENTRE - CRM System"
echo "================================================"
echo ""

# Check dependencies
if ! command -v dotnet &> /dev/null; then
  echo "ERROR: .NET SDK not found. Install from https://dotnet.microsoft.com/download"
  exit 1
fi

if ! command -v node &> /dev/null; then
  echo "ERROR: Node.js not found. Install from https://nodejs.org"
  exit 1
fi

# Install frontend dependencies if needed
if [ ! -d "$FRONTEND_DIR/node_modules" ]; then
  echo "Installing frontend dependencies..."
  cd "$FRONTEND_DIR" && npm install
fi

# Kill any processes already on these ports
echo "Clearing ports 5000 and 4200..."
lsof -ti:5000,4200 | xargs kill -9 2>/dev/null
sleep 1

# Backup database before starting
if [ -f "$BACKEND_DIR/pfc.db" ]; then
  cp "$BACKEND_DIR/pfc.db" "$BACKEND_DIR/pfc_backup_$(date +%Y%m%d_%H%M%S).db"
  echo "Database backed up."
fi

echo "Starting Backend  → http://localhost:5000"
echo "Starting Frontend → http://localhost:4200"
echo ""
echo "Default login: admin / admin123"
echo ""
echo "Press Ctrl+C to stop both servers."
echo ""

# Start backend
cd "$BACKEND_DIR" && dotnet run &
BACKEND_PID=$!

# Wait a moment then start frontend
sleep 2
cd "$FRONTEND_DIR" && NG_CLI_ANALYTICS=false CI=1 npx ng serve --open &
FRONTEND_PID=$!

# Handle Ctrl+C - kill both processes
trap "echo ''; echo 'Stopping servers...'; kill $BACKEND_PID $FRONTEND_PID 2>/dev/null; exit 0" SIGINT SIGTERM

# Wait for both
wait $BACKEND_PID $FRONTEND_PID
