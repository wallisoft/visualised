# Contributing to Visualised

## For Individuals & Non-Profits

Contributions welcome! This project thrives on collaboration.

### Getting Started

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/amazing-feature`
3. Make your changes
4. Test thoroughly: `dotnet build && dotnet run`
5. Commit: `git commit -m "Add amazing feature"`
6. Push: `git push origin feature/amazing-feature`
7. Open a Pull Request

### Code Style

- Follow existing patterns (see MainWindow.axaml.cs)
- Use ghost controls (Border dummies) not real controls
- Keep VML simple and readable
- Comment non-obvious logic
- Test drag-and-drop thoroughly

### Database Changes

All code changes are versioned in `visualised.db`. Use the provided scripts:
- `./db-save.sh` - Save current state
- `./db-restore.sh` - Restore from DB

### Questions?

Open an issue or contact wallisoft@gmail.com

## For Commercial Contributors

Commercial contributions require a commercial license agreement.
Contact wallisoft@gmail.com to discuss.

