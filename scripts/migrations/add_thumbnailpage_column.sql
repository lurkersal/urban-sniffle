-- Migration: Add ThumbnailPage column to Article table
-- Date: 2026-04-23
-- Description: Adds ThumbnailPage field to store which page should be used for article thumbnail
--              Defaults to NULL (will use first page of article)

-- Add the column
ALTER TABLE Article ADD COLUMN ThumbnailPage INT;

-- Add comment explaining the column
COMMENT ON COLUMN Article.ThumbnailPage IS 'Page number to use for article thumbnail. NULL means use first page.';

