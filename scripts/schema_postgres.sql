-- Stores magazine titles
CREATE TABLE Magazine (
    MagazineId SERIAL PRIMARY KEY,
    Name VARCHAR(255) NOT NULL UNIQUE,
    LogoUrl VARCHAR(500)
);

-- Insert magazine titles
INSERT INTO Magazine (Name, LogoUrl) VALUES ('Mayfair', '/images/logos/mayfair.png');
INSERT INTO Magazine (Name, LogoUrl) VALUES ('Knave', 'https://www.knave.co.uk/wp-content/uploads/2020/04/cropped-knave-logo-1.png');
INSERT INTO Magazine (Name, LogoUrl) VALUES ('Fiesta', '/images/logos/fiesta.jpg');
INSERT INTO Magazine (Name, LogoUrl) VALUES ('Club International', '/images/logos/club-international.jpg');
INSERT INTO Magazine (Name) VALUES ('Men Only');
INSERT INTO Magazine (Name, LogoUrl) VALUES ('Razzle', '/images/logos/razzle.jpg');

-- Stores content Category
CREATE TABLE Category (
    CategoryId SERIAL PRIMARY KEY,
    Name VARCHAR(100) NOT NULL UNIQUE
);

-- Insert category types
INSERT INTO Category (Name) VALUES ('Group');
INSERT INTO Category (Name) VALUES ('Cover');
INSERT INTO Category (Name) VALUES ('Index');
INSERT INTO Category (Name) VALUES ('Editorial');
INSERT INTO Category (Name) VALUES ('Cartoons');
INSERT INTO Category (Name) VALUES ('Letters');
INSERT INTO Category (Name) VALUES ('Wives');
INSERT INTO Category (Name) VALUES ('Model');
INSERT INTO Category (Name) VALUES ('Pinup');
INSERT INTO Category (Name) VALUES ('Fiction');
INSERT INTO Category (Name) VALUES ('Feature');
INSERT INTO Category (Name) VALUES ('Humour');
INSERT INTO Category (Name) VALUES ('Motoring');
INSERT INTO Category (Name) VALUES ('Travel');
INSERT INTO Category (Name) VALUES ('Review');
INSERT INTO Category (Name) VALUES ('Illustrations');
INSERT INTO Category (Name) VALUES ('Interview');

-- Stores magazine issues
CREATE TABLE Issue (
    IssueId SERIAL PRIMARY KEY,
    MagazineId INT NOT NULL,
    Volume INT NOT NULL,
    Number INT NOT NULL,
    Year INT NOT NULL,
    LinkScanPerformed BOOLEAN NOT NULL DEFAULT FALSE,
    FOREIGN KEY (MagazineId) REFERENCES Magazine(MagazineId),
    UNIQUE(MagazineId, Volume, Number)
);

-- Stores articles with metadata
CREATE TABLE Article (
    ArticleId SERIAL PRIMARY KEY,
    CategoryId INT NOT NULL,
    Title VARCHAR(255),
    FOREIGN KEY (CategoryId) REFERENCES Category(CategoryId)
);

-- Stores page-level content entries linking to articles and issues
CREATE TABLE Content (
    ContentId SERIAL PRIMARY KEY,
    IssueId INT NOT NULL,
    Page INT NOT NULL,
    ArticleId INT NOT NULL,
    ImagePath VARCHAR(500),
    FOREIGN KEY (IssueId) REFERENCES Issue(IssueId),
    FOREIGN KEY (ArticleId) REFERENCES Article(ArticleId)
);

-- Stores contributors (photographers, authors, illustrators, etc.)
CREATE TABLE Contributor (
    ContributorId SERIAL PRIMARY KEY,
    Name VARCHAR(255) NOT NULL,
    UNIQUE(Name)
);

-- Junction table for many-to-many relationship between content and contributors
CREATE TABLE ContentContributor (
    ContentContributorId SERIAL PRIMARY KEY,
    ContentId INT NOT NULL,
    ContributorId INT NOT NULL,
    FOREIGN KEY (ContentId) REFERENCES Content(ContentId),
    FOREIGN KEY (ContributorId) REFERENCES Contributor(ContributorId),
    UNIQUE(ContentId, ContributorId)
);

-- Stores models featured in magazine
CREATE TABLE Model (
    ModelId SERIAL PRIMARY KEY,
    Name VARCHAR(255) NOT NULL UNIQUE,
    YearOfBirth INT,
    BustSize INT,
    WaistSize INT,
    HipSize INT,
    CupSize VARCHAR(5)
);

-- Junction table for content that features models
-- Age is stored here because it's article-specific (often made up)
CREATE TABLE ContentModel (
    ContentModelId SERIAL PRIMARY KEY,
    ArticleId INT NOT NULL,
    ModelId INT NOT NULL,
    Age INT,
    Measurements TEXT,
    FOREIGN KEY (ArticleId) REFERENCES Article(ArticleId),
    FOREIGN KEY (ModelId) REFERENCES Model(ModelId),
    UNIQUE(ArticleId, ModelId)
);

-- Stores links between a page (Content) and many linked issues (by magazine, volume, number)

CREATE TABLE IssueLink (
    ContentId INT NOT NULL REFERENCES Content(ContentId) ON DELETE CASCADE,
    LinkedMagazineId INT NOT NULL REFERENCES Magazine(MagazineId),
    LinkedVolume INT NOT NULL,
    LinkedNumber INT NOT NULL,
    Page INT,
    PRIMARY KEY (Page, LinkedVolume, LinkedNumber, LinkedMagazineId)
);

CREATE INDEX idx_issuelink_contentid ON IssueLink(ContentId);
CREATE INDEX idx_issuelink_linkedmagazineid ON IssueLink(LinkedMagazineId);
-- Magazine Parser Database Schema (PostgreSQL)

-- Indexes for common queries
CREATE INDEX IDX_MagazineName ON Magazine(Name);
CREATE INDEX IDX_ArticleTitle ON Article(Title);
CREATE INDEX IDX_ArticleCategoryId ON Article(CategoryId);
CREATE INDEX IDX_ContentIssueId ON Content(IssueId);
CREATE INDEX IDX_ContentArticleId ON Content(ArticleId);
CREATE INDEX IDX_ModelName ON Model(Name);
CREATE INDEX IDX_ContributorName ON Contributor(Name);
CREATE INDEX IDX_IssueId ON Content(IssueId);
