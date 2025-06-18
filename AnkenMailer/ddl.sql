-- Script Date: 2025/05/31 17:07  - ErikEJ.SqlCeScripting version 3.5.2.95
DROP TABLE IF EXISTS [Envelope];
CREATE TABLE [Envelope] (
  [EnvelopeId] INTEGER PRIMARY KEY
, [MessageId] TEXT NOT NULL
, [Date] TEXT NOT NULL
, [From] TEXT NOT NULL
, [Bcc] TEXT NULL
, [Cc] TEXT NULL
, [InReplyTo] TEXT NULL
, [ReplyTo] TEXT NULL
, [Sender] TEXT NOT NULL
, [Subject] TEXT NOT NULL
, [To] TEXT NOT NULL
);
CREATE INDEX index_envelope_01 ON envelope([MessageId], [From]);

DROP TABLE IF EXISTS [Message];
CREATE TABLE [Message] (
  [EnvelopeId] INTEGER NOT NULL
, [Body] TEXT NOT NULL
, CONSTRAINT [PK_Message] PRIMARY KEY ([EnvelopeId])
, FOREIGN KEY ([EnvelopeId])
    REFERENCES [Envelope]([EnvelopeId])
    ON DELETE CASCADE
    ON UPDATE CASCADE
);

DROP TABLE IF EXISTS [AnkenHeader];
CREATE TABLE [AnkenHeader] (
  [EnvelopeId] INTEGER NOT NULL
  , [HasError] BOOLEAN NOT NULL DEFAULT true
  , [ErrorMessage]  TEXT NULL
  , [JSON]  TEXT NULL
  , [CeateDateTime]  TEXT NULL
, CONSTRAINT [AnkenHeader] PRIMARY KEY ([EnvelopeId])
, FOREIGN KEY ([EnvelopeId])
    REFERENCES [Envelope]([EnvelopeId])
    ON DELETE CASCADE
    ON UPDATE CASCADE
);

DROP TABLE IF EXISTS [Anken];
CREATE TABLE [Anken] (
  [EnvelopeId] INTEGER NOT NULL
, [Index]  INTEGER NOT NULL
, [Name] TEXT NULL -- 案件の名前
, [Start] TEXT NULL -- 案件の終了時期。表記内容を自然文でそのまま抽出してください。
, [End] TEXT NULL -- 案件の終了時期。表記内容を自然文でそのまま抽出してください。
, [StartYearMonth] TEXT NULL -- 案件の開始時期。内容を解釈し、YYYY-MM形式で出力してください。
, [Place] TEXT NULL -- 作業場所
, [Details] TEXT NULL -- 作業内容。複数存在する場合は、連結してひとつの文字列にしてください。
, [MainSkill] TEXT NULL -- 主な開発言語として、"JAVA",".NET", "iOS", "Android", "Pytion", "Ruby","それ以外"のどれかを選択してください。
, [RequiredSkills] TEXT NULL -- 必須の技術スタックの一覧。
, [DesirableSkills] TEXT NULL -- あると有利な技術スタックの一覧。
, [MaxUnitPrice] INTEGER NULL -- 単価の最大
, [MinUnitPrice] INTEGER NULL -- 単価の最小
, [Remarks] TEXT NULL -- 備考
, CONSTRAINT [PK_Anken] PRIMARY KEY ([EnvelopeId], [Index])
, FOREIGN KEY ([EnvelopeId])
    REFERENCES [AnkenHeader]([EnvelopeId])
    ON DELETE CASCADE
    ON UPDATE CASCADE
);


DROP TABLE IF EXISTS [TotalizationTarget];
CREATE TABLE [TotalizationTarget] (
  [EnvelopeId] INTEGER NOT NULL
, CONSTRAINT [TotalizationTarget] PRIMARY KEY ([EnvelopeId])
, FOREIGN KEY ([EnvelopeId])
    REFERENCES [Envelope]([EnvelopeId])
    ON DELETE CASCADE
    ON UPDATE CASCADE
);

DROP TABLE IF EXISTS [MainSkill];
CREATE TABLE [MainSkill] (
    [SkillName] TEXT NULL PRIMARY KEY
);
CREATE INDEX index_skill_01 ON MainSkill([SkillName]);

