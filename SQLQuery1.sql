﻿CREATE TABLE email_codes (
    id UNIQUEIDENTIFIER        PRIMARY KEY    DEFAULT NEWID(),
    email NVARCHAR(128)        NOT NULL,
    code  CHAR(6)              NULL
)