# PROG-PART-3: BOTBUDDY CYBERSECURITY CHATBOT

## Project Overview

BotBuddy is a dark cyberpunk-themed WPF desktop application designed as an interactive educational chatbot for cybersecurity learning. The application combines conversational AI with gamified features including quizzes, mini-games, reminder/task management, and activity logging to create an engaging cybersecurity education platform.

### Key Features

- Interactive cybersecurity chatbot with sentiment detection
- Quiz system and mini-game challenges
- Task Assistant with reminder management and calendar integration
- Activity Log for tracking user interactions
- Cyberpunk-themed dark UI with neon pink (#FF1493) and cyan accents
- User authentication system
- Audio-enhanced splash screen experience
- Memory/personalization system for contextual responses


## Software Requirements

### Required Software

- Visual Studio 2019 or later (Community, Professional, or Enterprise edition)
- .NET Framework 4.7.2 or higher (or .NET 5.0/6.0+ if using modern WPF)
- MySQL Server 5.7 or higher (for local database)
- MySQL Connector/NET 8.0 or higher (NuGet package)

### Recommended Tools

- MySQL Workbench (for database management)
- Git (for version control)
- Visual Studio Extensions: XAML Designer, NuGet Package Manager


## Getting Started

### 1. Installation & Setup

#### Step 1: Clone the Repository
```
git clone https://github.com/yourusername/BOTBUDDY_CYBERSECURITY_CHATBOT.git
cd BOTBUDDY_CYBERSECURITY_CHATBOT
```

#### Step 2: Install NuGet Dependencies
Open the BOTBUDDY CYBERSECURITY CHATBOT.csproj file in Visual Studio.

Go to Tools → NuGet Package Manager → Package Manager Console and run:
```
dotnet restore
```

Alternatively, right-click the solution and select Restore NuGet Packages.

#### Step 3: Build and Run
- Press F5 in Visual Studio to build and run.
- The splash screen will play a short animation and audio (if BOTBUDDY.wav is present in the output folder).
- The main chat interface appears after the splash.


## Database Setup Instructions

BotBuddy uses a MySQL database to persist tasks and reminders.

### 1. Install MySQL Server
Ensure MySQL Server is installed and running on your machine.

### 2. Create the Database
Open MySQL Workbench or command line and run:
```sql
CREATE DATABASE botbuddy_db;
```

### 3. Update Connection String
Open TaskRepository.cs and update the connection string (around line 12):
```csharp
string server = "localhost";
string database = "botbuddy_db";
string username = "root";
string password = "YOUR_PASSWORD_HERE";
```

Replace YOUR_PASSWORD_HERE with your actual MySQL root password.

### 4. Auto-Create Table
The application will automatically create the Tasks table on first launch. The table schema is:
```sql
CREATE TABLE IF NOT EXISTS Tasks (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Title VARCHAR(255) NOT NULL UNIQUE,
    Description TEXT,
    ReminderDate DATETIME,
    IsCompleted BOOLEAN DEFAULT FALSE,
    Category VARCHAR(50) DEFAULT 'Task',
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

Note: If the Category column is missing, the app will attempt to add it automatically.


## How to Use the Task Assistant

The Task Assistant lets you manage cybersecurity‑related tasks and reminders directly from the chat.

### Natural Language Commands

| Action | Example |
|--------|---------|
| Add a task | add task review firewall settings |
| Add a task with a due date | add task update password in 3 days |
| Set a reminder | remind me to backup files tomorrow |
| Complete a task | complete task 5 or complete task update password |
| Delete a task | delete task 3 or delete task backup files |
| Delete all tasks | delete all tasks or clear tasks |
| Delete a reminder | delete reminder update antivirus |
| Delete all reminders | delete all reminders |
| Delete completed tasks | delete completed all or clear completed |
| View summary | show summary or summary |
| View only reminders | show reminders |
| View only tasks | show tasks |

Important: All tasks must be cybersecurity‑related (the bot validates this).

### Date Parsing Examples
The bot understands natural language dates:
- tomorrow
- next week
- in 3 days
- in 2 weeks
- on Monday
- on the 15th
- 5th of August

### Interactive Buttons
When adding a task without a date, the bot will display inline buttons (YES / NO) asking if you want to set a reminder.
If you choose YES, a date picker calendar appears – select a date and click SUBMIT DATE.

All tasks are stored in the MySQL database and survive application restarts.


## How to Access the Quiz / Mini-Game

BotBuddy includes a cybersecurity quiz to test your knowledge.

### Ways to Start the Quiz

1. Click the avatar (top‑right corner) → select "TAKE QUIZ".
2. Type one of the following in the chat:
   - game
   - quiz
   - quiz game
   - test me
   - start quiz

### Quiz Modes

After starting, choose a difficulty:
- QUICK – 5 questions (2‑3 min)
- BALANCED – 15 questions (5‑8 min)
- DEEP – 30 questions (10‑15 min)

### Quiz Interface

- Multiple‑choice or true/false questions.
- Immediate feedback with explanations.
- Progress bar and timer.
- Final score card with percentage and performance feedback.

You can quit at any time using the ✕ button on the quiz card.


## How to Test the NLP Simulation

The chatbot simulates natural language understanding using a custom keyword‑matching engine and state tracking—no external APIs are used.

### Supported Query Types

| Category | Example Questions |
|----------|-------------------|
| Greetings | Hello, Hi, Good morning |
| How are you | How are you?, What about you? |
| What is X | What is phishing?, Define malware |
| Examples | Give me an example of ransomware (or example after definition) |
| Tips | Give me a tip about passwords (or tip after definition) |
| More details | Tell me more about 2FA (or more after definition) |
| Name setting | My name is Alice |
| Interest statements | I like encryption, I'm interested in cloud security |
| Help | Help, What can you help with |

### How the NLP Simulation Works

1. Keyword Matching – The KeywordMatcher class uses predefined sets to classify input.
2. Cyber Term Detection – The CybersecurityKnowledgeBase contains a dictionary of terms with definitions (Part1, Part2, Part3) and tips.
3. State Management – ConversationStateTracker remembers the last topic, definition parts shown, and follow‑up context.
4. Follow‑Up Handling – The FollowUpHandler recognises requests like "tell me more", "another tip", "example" and picks the next logically correct response.
5. Emotion Detection – Words like "worried", "scared", "frustrated" trigger empathetic responses.

To test it, ask a question, then use follow‑up phrases (e.g., example, more, tip) to see context awareness.


## How to View the Activity Log

The Activity Log records all major user actions and system events.

### Access Methods

1. Click the avatar (top‑right) → "SHOW ACTIVITY LOG".
   - This displays the last 10 entries in the chat.
   - Use the "SEE MORE" button to view all entries, or "SEE LESS" to return to the last 5.
2. Type in chat:
   - show activity log – shows last 10.
   - show full log – shows all entries.
   - clear log – clears the log.

The log entries include timestamps, action names, and optional details (e.g., topic for definitions, task descriptions).


## Login Details & Important Notes

### Default Account

On first launch, the application has a hard‑coded Guest account:

- Username: Guest
- Password: Aa1!

This account is automatically logged in.

### Registration / Login

- You can register a new account by clicking "Register here" on the login panel.
- Registration rules:
  - Username: minimum 2 letters, only alphabetic characters, unique.
  - Password: exactly 4 characters – one lowercase, one uppercase, one digit, one symbol (e.g., Aa1!).
- After registration, you can log in with the new credentials.

### Recycle Bin

- Deleted tasks, reminders, and conversation threads are moved to the Recycle Bin (accessible via the sidebar button or by typing recycle bin in chat).
- You can restore or permanently delete items, with multi‑select support.

### Important Notes

- Database credentials – The connection string is hard‑coded in TaskRepository.cs. Change it before deploying to production.
- Audio file – The splash screen attempts to play BOTBUDDY.wav from the output directory. If missing, the app continues without audio.
- All user accounts and tasks are stored in the local MySQL database – ensure the database is running before starting the app.


## Video Presentation Link

https://youtu.be/CleNITnow8w

## Contributing

Contributions are welcome! To contribute:
1. Fork the repository.
2. Create a new branch (git checkout -b feature/your-feature).
3. Commit your changes (git commit -m 'Add some feature').
4. Push to the branch (git push origin feature/your-feature).
5. Open a Pull Request.

Please ensure your code follows the existing style and passes all tests.


## License

This project is licensed under the MIT License – see the LICENSE file for details.


## Acknowledgements

- Built as part of the PROG‑PART‑3 assignment.
- Thanks to the open‑source community for the amazing tools.

