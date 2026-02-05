
            SELECT 
                mc.ContentId, mc.IssueId, mc.Page, mc.ArticleId, mc.ImagePath,
                mag.Name as MagazineName, mag.MagazineId, i.Volume, i.Number, i.Year,
                (mag.Name || ' ' || i.Volume || '-' || LPAD(i.Number::text, 2, '0')) as IssueName,
                c.Name as CategoryName, a.Title,
                STRING_AGG(CASE WHEN cont.Role = 'Photographer' THEN cont.Name ELSE NULL END, ' | ') as Photographer,
                m.Name as ModelName, cm.Age as ModelAge,
                CASE WHEN m.BustSize IS NOT NULL 
                    THEN CAST(m.BustSize AS VARCHAR) || COALESCE(m.CupSize, '') || '-' || CAST(m.WaistSize AS VARCHAR) || '-' || CAST(m.HipSize AS VARCHAR) 
                    ELSE NULL END as ModelMeasurements
            FROM Content mc
            JOIN Article a ON mc.ArticleId = a.ArticleId
            JOIN Category c ON a.CategoryId = c.CategoryId
            JOIN Issue i ON mc.IssueId = i.IssueId
            FROM content mc
            JOIN Article a ON mc.ArticleId = a.ArticleId
            JOIN Category c ON a.CategoryId = c.CategoryId
            JOIN Issue i ON mc.IssueId = i.IssueId
            JOIN Magazine mag ON i.MagazineId = mag.MagazineId
            JOIN ContentModel cm ON a.ArticleId = cm.ArticleId
            JOIN Model m ON cm.ModelId = m.ModelId
            LEFT JOIN ContentContributor cc ON mc.ContentId = cc.ContentId
            LEFT JOIN Contributor cont ON cc.ContributorId = cont.ContributorId
            WHERE m.ModelId = @ModelId
            GROUP BY mc.ContentId, mc.IssueId, mc.Page, mc.ArticleId, mc.ImagePath,
                mag.Name, mag.MagazineId, i.Volume, i.Number, i.Year,
                c.Name, a.Title, m.Name, cm.Age, m.BustSize, m.CupSize, m.WaistSize, m.HipSize
            ORDER BY i.Year, i.Volume, i.Number, mc.Page
