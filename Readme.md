---
title: setting up a programmatic parser for custom policies in Azure Active Directory B2C | Microsoft Docs
description: This article will show you how to use MSGraph and the XSD schema for trust framework policy to make changes and control policy.
services: active-directory-b2c
author: valnav
manager: 

ms.service: active-directory
ms.workload: identity
ms.topic: conceptual
ms.date: 04/14/2019
ms.author: nvalluri
ms.subservice: B2C

---

# Introduction
This sample has 2 projects - Parser and the Client. The parser is a library that enables adding and removing relying party policies, adding and removing identity providers. The policies inherit from a TrustFramework Base Policy already running on a tenant.

# How to Run the client
The `Program.cs` calls the `B2CClientParser`. The command line option to be passed are:

- pass an integer (default)0, 1 or 2 or 3 
- New Tenant Scenario : 0
- Existing Tenant Scenario : 1
- Existing Tenant of 1P Scenario : 2
- Test Auth for the above scenarios: 3
