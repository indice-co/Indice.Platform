# Clean and rebuild Angular project
Write-Host "🧹 Cleaning project..." -ForegroundColor Cyan

rmdir node_modules -Recurse -Force -ErrorAction SilentlyContinue
rm package-lock.json -Force -ErrorAction SilentlyContinue
rmdir .angular -Recurse -Force -ErrorAction SilentlyContinue

Write-Host "📦 Installing dependencies..." -ForegroundColor Yellow
npm install

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ npm install failed!" -ForegroundColor Red
    exit 1
}

Write-Host "🔨 Building project..." -ForegroundColor Yellow
ng build

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Done!" -ForegroundColor Green