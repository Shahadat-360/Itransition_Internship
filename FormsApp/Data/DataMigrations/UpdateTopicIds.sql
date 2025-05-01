-- Update existing FormTemplates to set TopicId to NULL
-- since predefined topics have been removed

UPDATE FormTemplates SET TopicId = NULL; 