# Security Policy

This Security Policy ("Policy") governs the security practices, responsibilities, and procedures for the Loft e-commerce platform ("Loft" or "Platform"), an open-source internet store enabling secure buying, selling, and transaction processing.
This Policy is effective as of October 1, 2025, and applies to all users, contributors, and third parties interacting with the Platform.
Purpose: The Policy aims to protect user data, ensure operational integrity, and mitigate risks associated with e-commerce activities, including but not limited to data breaches, unauthorized access, payment fraud, and supply chain vulnerabilities. 
By establishing clear guidelines, Loft seeks to comply with applicable laws and minimize legal exposure for the startup.
Disclaimer: This Policy is a guideline for internal and external use and does not constitute legal advice. Loft is not liable for any damages arising from its use or reliance thereon. Users and contributors agree to consult qualified legal professionals for jurisdiction-specific advice. 
Loft reserves the right to update this Policy at any time, with material changes notified via the GitHub repository or email. Continued use constitutes acceptance.

This Policy applies to:

All code, documentation, assets, and releases in the Loft GitHub repository.
All Team members, contributors (including open-source participants), users (buyers/sellers), and administrators.
Platform features: user registration, authentication, product listings, payment processing, order fulfillment, data storage, and third-party integrations (e.g., payment gateways like Stripe).
Deployments: Production, staging, and development environments, including containerized setups (e.g., Docker).
Exclusions: User-generated content (e.g., product descriptions) is governed by the Platform's Terms of Service, not this Policy.

The Policy addresses e-commerce-specific risks per OWASP Top 10, including injection attacks, broken authentication, sensitive data exposure, and XML external entities.

Contributor Responsibilities:

Adhere to secure contribution guidelines (e.g., no hard-coded secrets); report vulnerabilities responsibly.
Obtain explicit permission for any modifications to core security features.

User Responsibilities (Policy of Use):

Use the Platform only for lawful purposes; comply with all applicable laws (e.g., no fraudulent transactions, no prohibited goods).
Maintain account security: Use strong, unique passwords; enable multi-factor authentication (MFA) where available; do not share credentials.
Report suspicious activity promptly; do not attempt unauthorized access or reverse engineering.
For sellers: Ensure product listings comply with intellectual property laws; handle personal data ethically.
For buyers: Provide accurate information; do not engage in chargeback abuse.
Prohibited Uses: Automated scraping, DDoS attacks, malware distribution, or any activity violating this Policy. Violations may result in account suspension, legal action, or reporting to authorities.
Data Handling: Users consent to data collection for transaction purposes; Loft processes data per privacy policy (linked in Terms of Service).

Third-Party/Vendor Responsibilities:

Comply with PCI-DSS for payment integrations; provide SOC 2 reports annually.
Sign data processing agreements (DPAs) outlining security obligations.

Failure to comply may result in termination of access, indemnity claims, or legal recourse. Loft disclaims liability for user-induced breaches.


## Supported Versions

| Version | Supported          |
| ------- | ------------------ |
|  1.0    | :white_check_mark: |


## Reporting a Vulnerability

How to Report

Preferred Method: Open a new issue on this GitHub repository. Use the "Security Vulnerability" template (if available) or include the following details:

A clear description of the vulnerability.
Steps to reproduce (include code snippets or screenshots if possible).
Impact assessment (e.g., potential data exposure, privilege escalation, or financial risks).
Affected versions.
Any suggested mitigation or patch.
Label the issue with security for quick triage.

What to Expect

Acknowledgment: We'll confirm receipt within 48 hours and assign a priority based on severity (using CVSS scoring where applicable).
Updates: Expect status updates every 7-14 days via the issue thread or email. For high-severity issues (e.g., those affecting payment processing or user authentication), we'll aim for a fix within 30 days.
Resolution:

If accepted: We'll credit you in the release notes (with your permission).
If declined (e.g., not reproducible or low impact): We'll explain why and close the issue.


Post-Disclosure: Once patched, we'll publish a security advisory on this repo and notify affected users if necessary.

We reserve the right to reject reports that violate our guidelines (e.g., spam or known issues).
