UPDATE tbl_jobticket
SET ClientFullName = '', PrimaryNumber = '', SecondaryNumber = NULL
WHERE JobType = 'Maintenance';

select * from tbl_jobticket